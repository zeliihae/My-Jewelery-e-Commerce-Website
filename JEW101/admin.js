
// ==========================================
// 1. GLOBAL CONFIGURATION & STATE
// ==========================================
const API_URL = 'http://localhost:5025/api'; // Base API URL
const API_ADMIN = `${API_URL}/admin`;       // Admin specific endpoints

let currentSection = 'dashboard';
let products = [];
let categories = [];
let orders = [];
let coupons = [];
let editingProductId = null;
let editingCategoryId = null;
let editingCouponId = null;

// ==========================================
// 2. INITIALIZATION
// ==========================================
document.addEventListener('DOMContentLoaded', () => {
    initializeNavigation();
    initializeModals();
    initializeForms();
    
    // Initial data load
    loadDashboardData();
    loadProducts();
    loadCategories();
    loadOrders();
    loadUsers();
    loadCoupons();
});

// ==========================================
// 3. NAVIGATION LOGIC
// ==========================================
function initializeNavigation() {
    const navLinks = document.querySelectorAll('.nav-link[data-section]');
    const sidebar = document.querySelector('.sidebar');

    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const section = link.getAttribute('data-section');
            switchSection(section);
            
            navLinks.forEach(l => l.classList.remove('active'));
            link.classList.add('active');
            
            if (window.innerWidth <= 768) sidebar.classList.remove('active');
        });
    });

    const menuToggle = document.getElementById('menuToggle');
    if (menuToggle) {
        menuToggle.addEventListener('click', () => {
            sidebar.classList.toggle('active');
        });
    }
}

function switchSection(section) {
    const sections = document.querySelectorAll('.content-section');
    sections.forEach(s => s.classList.remove('active'));
    
    const targetSection = document.getElementById(section);
    if (targetSection) {
        targetSection.classList.add('active');
        currentSection = section;
        
        const pageTitle = document.querySelector('.page-title');
        if (pageTitle) {
            const titles = {
                'dashboard': 'Dashboard',
                'products': 'Ürün Yönetimi',
                'categories': 'Kategori Yönetimi',
                'orders': 'Sipariş Yönetimi',
                'users': 'Kullanıcı Yönetimi',
                'coupons': 'Kupon Yönetimi'
            };
            pageTitle.textContent = titles[section] || 'Admin Panel';
        }
        
        // Refresh data when switching to coupons
        if (section === 'coupons') loadCoupons();
    }
}

// ==========================================
// 4. DASHBOARD DATA
// ==========================================
async function loadDashboardData() {
    try {
        const token = localStorage.getItem('jewelry_token');
        const headers = { 'Authorization': `Bearer ${token}` };

        // Fetch multiple stats in parallel
        const [pRes, oRes, uRes, cRes] = await Promise.all([
            fetch(`${API_ADMIN}/Products/stats`),
            fetch(`${API_URL}/Orders/admin/all`),
            fetch(`${API_ADMIN}/Users`),
            fetch(`${API_URL}/Coupons/stats`, { headers })
        ]);

        const pData = await pRes.json();
        const oData = await oRes.json();
        const uData = await uRes.json();
        const cData = await cRes.json();

        // Update UI
        document.getElementById('totalProducts').textContent = pData.totalProducts || 0;
        if (document.getElementById('totalUsers')) document.getElementById('totalUsers').textContent = uData.data?.length || 0;
        if (document.getElementById('totalCoupons')) document.getElementById('totalCoupons').textContent = cData.totalCoupons || 0;

        if (oData.success && oData.data) {
            const revenue = oData.data.reduce((sum, order) => sum + parseFloat(order.orderTotal || 0), 0);
            document.getElementById('totalRevenue').textContent = `₺${revenue.toFixed(2)}`;
            document.getElementById('totalOrders').textContent = oData.data.length;
        }
    } catch (error) {
        console.error('Dashboard Load Error:', error);
    }
}


async function loadCoupons() {
    try {
        const token = localStorage.getItem('jewelry_token');
        const response = await fetch(`${API_URL}/Coupons`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (!response.ok) throw new Error('Kuponlar yüklenemedi');
        coupons = await response.json();
        renderCouponsTable();
    } catch (error) {
        console.error('Coupon Load Error:', error);
    }
}

function renderCouponsTable() {
    const tbody = document.getElementById('couponsTableBody');
    if (!tbody) return;

    if (!coupons || coupons.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" style="text-align: center; padding: 40px; color: #666;">Henüz tanımlı kupon bulunmuyor.</td></tr>';
        return;
    }

    tbody.innerHTML = coupons.map(coupon => {
        // Kupon tipi gösterimi
        const isPercentage = coupon.couponType === 'Percentage' || coupon.couponType === 0;
        const discountDisplay = `${coupon.discountValue}${isPercentage ? '%' : ' ₺'}`;
        
        return `
            <tr>
                <td style="width: 150px;"><strong style="color:#8B2F39; font-size: 1rem;">${coupon.couponCode}</strong></td>
                <td><span style="font-weight: 600;">${discountDisplay}</span></td>
                <td>
                    <span class="status-badge ${isPercentage ? 'info' : 'warning'}" style="font-size: 0.75rem;">
                        ${isPercentage ? 'Yüzde' : 'Sabit'}
                    </span>
                </td>
                <td>₺${coupon.minOrderAmount.toFixed(2)}</td>
                <td>
                    <div style="display: flex; align-items: center; gap: 5px;">
                        <progress value="${coupon.usedCount}" max="${coupon.usageLimit || 100}" style="width: 50px; height: 6px;"></progress>
                        <small>${coupon.usedCount} / ${coupon.usageLimit || '∞'}</small>
                    </div>
                </td>
                <td><small>${new Date(coupon.validFrom).toLocaleDateString('tr-TR')}</small></td>
                <td>
                    <span class="status-badge ${coupon.isActive ? 'completed' : 'cancelled'}">
                        ${coupon.isActive ? 'Aktif' : 'Pasif'}
                    </span>
                </td>
                <td style="text-align: right; width: 100px;">
                    <div style="display: flex; gap: 5px; justify-content: flex-end;">
                        <button class="btn-edit" onclick="toggleCouponStatus(${coupon.couponId})" title="Durumu Değiştir">
                            <i class="fas fa-sync-alt"></i>
                        </button>
                        <button class="btn-delete" onclick="deleteCoupon(${coupon.couponId})" title="Sil">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
    }).join('');
}

async function saveCoupon() {
    const token = localStorage.getItem('jewelry_token');
    const endDate = document.getElementById('couponEndDate').value;
    
    const formData = {
        couponCode: document.getElementById('couponCode').value.toUpperCase(),
        couponType: document.getElementById('couponType').value === 'percentage' ? 0 : 1,
        discountValue: parseFloat(document.getElementById('couponDiscount').value),
        minOrderAmount: parseFloat(document.getElementById('couponMinAmount').value) || 0,
        usageLimit: document.getElementById('couponMaxUses').value ? parseInt(document.getElementById('couponMaxUses').value) : null,
        validFrom: document.getElementById('couponStartDate').value,
        validUntil: endDate || null
    };

    try {
        const response = await fetch(`${API_URL}/Coupons`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
            body: JSON.stringify(formData)
        });

        if (response.ok) {
            showNotification('Kupon başarıyla eklendi', 'success');
            closeModal('couponModal');
            loadCoupons();
            loadDashboardData();
        } else {
            const errText = await response.text();
            showNotification(`Hata: ${errText}`, 'error');
        }
    } catch (error) {
        showNotification('Sunucu hatası', 'error');
    }
}


window.toggleCouponStatus = async function(id) {
    const token = localStorage.getItem('jewelry_token');
    try {
        const response = await fetch(`${API_URL}/Coupons/${id}/toggle`, {
            method: 'PATCH',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            showNotification('Kupon durumu güncellendi', 'success');
            loadCoupons(); // Listeyi yenile
        } else {
            showNotification('Durum güncellenemedi', 'error');
        }
    } catch (error) {
        console.error('Hata:', error);
    }
};

window.deleteCoupon = async function(id) {
    if (!confirm('Silmek istediğinize emin misiniz?')) return;
    
    const token = localStorage.getItem('jewelry_token');
    try {
        const response = await fetch(`${API_URL}/Coupons/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (response.ok) {
            showNotification('Kupon silindi', 'success');
            loadCoupons();
        } else {
            const data = await response.json();
            showNotification(data.message || 'Hata oluştu', 'error');
        }
    } catch (error) {
        console.error('Hata:', error);
    }
};
function generateCouponCode() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let result = '';
    for (let i = 0; i < 8; i++) {
        result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    document.getElementById('couponCode').value = result;
}

function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.style.cssText = `
        position: fixed; top: 20px; right: 20px; padding: 15px 25px; z-index: 10000;
        background: ${type === 'success' ? '#27ae60' : type === 'error' ? '#e74c3c' : '#3498db'};
        color: white; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        animation: slideIn 0.3s ease forwards;
    `;
    notification.textContent = message;
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease forwards';
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

// Initialize Modals and Forms (Generic)
function initializeModals() {
    document.querySelectorAll('.close-modal').forEach(btn => {
        btn.addEventListener('click', () => closeModal(btn.getAttribute('data-modal')));
    });
}
function initializeForms() {
    

    const categoryForm = document.getElementById('categoryForm');
    const addCategoryBtn = document.getElementById('addCategoryBtn');
const couponForm = document.getElementById('couponForm');
    const addCouponBtn = document.getElementById('addCouponBtn');
    const productbtn=document.getElementById('addProductBtn');
  const productForm = document.getElementById('productForm');

    if (addCouponBtn) {
        addCouponBtn.addEventListener('click', (e) => {
            e.preventDefault(); // Sayfa yenilenmesini durdur
            editingCouponId = null;
            if (couponForm) couponForm.reset();
            openModal('couponModal');
        });
    }

    if (couponForm) {
        couponForm.addEventListener('submit', async (e) => {
            e.preventDefault(); // FORMUN SAYFAYI YENİLEMESİNİ ENGELLER
            await saveCoupon();
        });
    }
    // Kategori Ekleme Butonu (Modalı Açma)
    if (addCategoryBtn) {
        addCategoryBtn.onclick = (e) => {
            e.preventDefault(); // Sayfa yenilenmesini engelle
            editingCategoryId = null;
            if (categoryForm) categoryForm.reset();
            openModal('categoryModal');
        };
    }

    // Kategori Formu Gönderme
    if (categoryForm) {
        categoryForm.onsubmit = async (e) => {
            e.preventDefault(); // FORMUN SAYFAYI YENİLEMESİNİ ENGELLER
            console.log("Kategori formu gönderiliyor...");
            await saveCategory();
        };
    }
    

    // Ürün Formu Gönderme (Eğer varsa)
  if(productbtn){
    productbtn.onclick=(e)=>{
        e.preventDefault();
        editingProductId=null;
        if(productForm) productForm.reset();
        openModal('productModal')
    }
  }
    if (productForm) {
        productForm.onsubmit = async (e) => {
            e.preventDefault();
            await saveProduct();
        };
    }
}
function openModal(id) { document.getElementById(id)?.classList.add('active'); }
function closeModal(id) { document.getElementById(id)?.classList.remove('active'); }


const styleEl = document.createElement('style');
styleEl.textContent = `
    @keyframes slideIn { from { transform: translateX(400px); opacity: 0; } to { transform: translateX(0); opacity: 1; } }
    @keyframes slideOut { from { transform: translateX(0); opacity: 1; } to { transform: translateX(400px); opacity: 0; } }
`;
document.head.appendChild(styleEl);


//Users kısmı-------------------------------start


let users = []; 

async function loadUsers() {
    try {
        const response = await fetch(`http://localhost:5025/api/admin/Users`);
        const result = await response.json();

        if (result.success && result.data) {
            
            users = result.data; 

            const tbody = document.getElementById('usersTableBody');
            if (!tbody) return;

            tbody.innerHTML = users.map(user => `
                <tr>
                    <td>${user.userName}</td>
                    <td>${user.userEmail}</td>
                    <td>${user.createdAt ? new Date(user.createdAt).toLocaleDateString('tr-TR') : '-'}</td>
                    <td>
                        <span class="status-badge ${user.userStatus === 1 ? 'completed' : 'cancelled'}">
                            ${user.userStatus === 1 ? 'Aktif' : 'Pasif'}
                        </span>
                    </td>
                    <td>
                        <button class="btn-edit" onclick="editUser(${user.userId})" title="Durumu Değiştir">
                            <i class="fas fa-sync-alt"></i>
                        </button>
                        <button class="btn-delete" onclick="deleteUser(${user.userId})" title="Sil">
                            <i class="fas fa-trash"></i>
                        </button>
                    </td>
                </tr>
            `).join('');
        }
    } catch (error) {
        console.error('Kullanıcılar yüklenemedi:', error);
        showNotification('Kullanıcı listesi alınamadı', 'error');
    }
}

async function editUser(id) {
    // 1. Mevcut kullanıcıyı listeden bul (Mevcut durumu öğrenmek için)
    // Not: 'users' dizisinin loadUsers fonksiyonunda doldurulduğundan emin olun.
    const user = users.find(u => u.userId === id);
    if (!user) {
        console.error("Kullanıcı listede bulunamadı.");
        return;
    }

    // 2. Durumu tersine çevir (Toggle): 1 ise 0, 0 ise 1 gönder
    const newStatus = user.userStatus === 1 ? 0 : 1;

    try {
        // 3. API İsteği
        const response = await fetch(`http://localhost:5025/api/admin/Users/${id}/status`, {
            method: 'PUT',
            headers: { 
                'Content-Type': 'application/json'
                // Admin yetkisi sayfa girişinde çözüldüğü için buraya Token eklemiyoruz
            },
            // Saf sayı gönderiyoruz (Swagger'daki integer($int32) beklentisi için)
            body: JSON.stringify(newStatus) 
        });

        if (response.ok) {
            showNotification(`Kullanıcı durumu güncellendi: ${newStatus === 1 ? 'Aktif' : 'Pasif'}`, 'success');
            await loadUsers(); // Tabloyu yeni verilerle tazele
        } else {
            const errorText = await response.text();
            showNotification('Güncelleme başarısız: ' + errorText, 'error');
        }
    } catch (error) {
        console.error('Kullanıcı durum güncelleme hatası:', error);
        showNotification('Bağlantı hatası oluştu.', 'error');
    }
}

async function deleteUser(id) {
    if (!confirm('Are you sure?')) return;

    try {
        const response = await fetch(`http://localhost:5025/api/admin/Users/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            showNotification('User Deleted', 'success');
            loadUsers();
            loadDashboardData();
        } else {
            showNotification('Silme işlemi başarısız oldu', 'error');
        }
    } catch (error) {
        console.error('Silme hatası:', error);
    }
}




// Products ----------------------------------------------------------------
async function loadProducts() {
    try {
        const response = await fetch(`${API_ADMIN}/Products?pageSize=100`);
        const data = await response.json();
        products = data.products || [];
        renderProductsTable();
    } catch (error) {
        console.error('Ürünler yüklenemedi:', error);
        showNotification('Ürünler yüklenemedi', 'error');
    }
}

function renderProductsTable() {
    const tbody = document.getElementById('productsTableBody');
    if (!tbody) return;

    if (products.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 40px;">Henüz ürün eklenmemiş</td></tr>';
        return;
    }

    tbody.innerHTML = products.map(product => `
        <tr>
            <td style="width: 80px; text-align: center;">
                <img src="images/${product.mainImage || product.productImage}" 
                     alt="${product.productName}"
                     style="width: 50px; height: 50px; object-fit: cover; border-radius: 4px; border: 1px solid #eee;">
            </td>
            <td>
                <div style="max-width: 250px; white-space: normal;">
                    <strong style="color: #2c3e50;">${product.productName}</strong><br>
                    <small style="color: #95a5a6;">ID: #${product.productId}</small>
                </div>
            </td>
            <td style="color: #7f8c8d;">${product.categoryName || 'Kategori Yok'}</td>
            <td>
                <div style="line-height: 1.2;">
                    ${product.productDiscountPrice ? 
                        `<span style="text-decoration: line-through; color: #95a5a6; font-size: 0.85rem;">₺${product.productPrice.toFixed(2)}</span><br>
                         <strong style="color: #e74c3c;">₺${product.productDiscountPrice.toFixed(2)}</strong>` :
                        `<strong style="color: #2c3e50;">₺${product.productPrice.toFixed(2)}</strong>`
                    }
                </div>
            </td>
            <td style="text-align: center;">
                <span class="status-badge ${product.productStock === 0 ? 'cancelled' : product.productStock <= 10 ? 'pending' : 'completed'}">
                    ${product.productStock} Adet
                </span>
            </td>
            <td style="width: 120px; text-align: right;">
                <div style="display: flex; gap: 5px; justify-content: flex-end;">
                    <button class="btn-edit" onclick="editProduct(${product.productId})" title="Düzenle" style="padding: 6px 10px;">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn-delete" onclick="deleteProduct(${product.productId})" title="Sil" style="padding: 6px 10px;">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </td>
        </tr>
    `).join('');
}



async function saveProduct() {
    const formData = {
        productName: document.getElementById('productName').value,
        productDescription: document.getElementById('productDescription').value,
        productPrice: parseFloat(document.getElementById('productPrice').value),
        productDiscountPrice: document.getElementById('productDiscountPrice').value ? 
            parseFloat(document.getElementById('productDiscountPrice').value) : null,
        productStock: parseInt(document.getElementById('productStock').value),
        productImage: document.getElementById('productImage').value,
        categoryId: document.getElementById('productCategory').value ? 
            parseInt(document.getElementById('productCategory').value) : null,
        productStatus: 1
    };

    try {
        const url = editingProductId ? 
            `${API_ADMIN}/Products/${editingProductId}` : 
            `${API_ADMIN}/Products`;
        
        const method = editingProductId ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData)
        });

        if (response.ok) {
            showNotification(editingProductId ? 'Ürün güncellendi' : 'Ürün eklendi', 'success');
            closeModal('productModal');
            await loadProducts();
            await loadDashboardData();
        } else {
            const error = await response.text();
            showNotification('Hata: ' + error, 'error');
        }
    } catch (error) {
        console.error('Ürün kaydedilemedi:', error);
        showNotification('Ürün kaydedilemedi', 'error');
    }
}

async function editProduct(id) {
    try {
        const response = await fetch(`${API_ADMIN}/Products/${id}`);
        const product = await response.json();
        
        editingProductId = id;
        
        document.getElementById('productName').value = product.productName;
        document.getElementById('productDescription').value = product.productDescription || '';
        document.getElementById('productPrice').value = product.productPrice;
        document.getElementById('productDiscountPrice').value = product.productDiscountPrice || '';
        document.getElementById('productStock').value = product.productStock;
        document.getElementById('productImage').value = product.productImage || '';
        document.getElementById('productCategory').value = product.categoryId || '';
        
        document.querySelector('#productModal .modal-header h2').textContent = 'Ürünü Düzenle';
        openModal('productModal');
    } catch (error) {
        console.error('Ürün yüklenemedi:', error);
        showNotification('Ürün yüklenemedi', 'error');
    }
}

async function deleteProduct(id) {
    if (!confirm('Bu ürünü silmek istediğinizden emin misiniz?')) return;

    try {
        const response = await fetch(`${API_ADMIN}/Products/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            showNotification('Ürün silindi', 'success');
            await loadProducts();
            await loadDashboardData();
        } else {
            const error = await response.text();
            showNotification('Hata: ' + error, 'error');
        }
    } catch (error) {
        console.error('Ürün silinemedi:', error);
        showNotification('Ürün silinemedi', 'error');
    }
}
//product end--------------------------------------------------------
// Categories-------------------------------------------------------
async function loadCategories() {
    try {
        // 'admin' takısı olmayan ana kategori endpoint'ini deneyin
        const response = await fetch(`http://localhost:5025/api/Categories`); 
        
        // Hata kontrolü ekleyelim
        if (!response.ok) {
            throw new Error(`Sunucu hatası: ${response.status}`);
        }

        const data = await response.json();
        
        // Gelen verinin yapısına göre ayıklama (ApiResponse yapısına uyumlu)
        categories = data.data || data.categories || data || [];
        
        renderCategoriesGrid();
        updateCategorySelect();
    } catch (error) {
        console.error('Kategoriler yüklenemedi:', error);
        showNotification('Kategoriler yüklenemedi: ' + error.message, 'error');
    }
}
function renderCategoriesGrid() {
    const grid = document.querySelector('.categories-grid');
    if (!grid) return;

    if (categories.length === 0) {
        grid.innerHTML = '<p style="text-align: center; padding: 40px; grid-column: 1/-1;">Henüz kategori eklenmemiş</p>';
        return;
    }

  grid.innerHTML = categories.map(category => `
    <div class="category-card" style="display: flex; flex-direction: column; align-items: center; padding: 20px; text-align: center;">
        <div class="category-icon" style="font-size: 2rem; color: #8B2F39; margin-bottom: 15px;">
            <i class="fas ${category.categoryIcon || 'fa-gem'}"></i>
        </div>
        <h3 style="margin-bottom: 5px; font-size: 1.1rem; color: #333;">${category.categoryName}</h3>
        <p style="color: #666; font-size: 0.9rem; margin-bottom: 15px;">
            <i class="fas fa-box-open" style="font-size: 0.8rem;"></i> ${category.productCount || 0} Ürün
        </p>
        <div class="category-actions" style="display: flex; gap: 10px; width: 100%; justify-content: center; border-top: 1px solid #eee; padding-top: 15px;">
            <button class="btn-edit" onclick="editCategory(${category.categoryId})" title="Düzenle">
                <i class="fas fa-edit"></i>
            </button>
            <button class="btn-delete" onclick="deleteCategory(${category.categoryId})" title="Sil">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    </div>
`).join('');
}

function updateCategorySelect() {
    const select = document.getElementById('productCategory');
    if (!select) return;

    select.innerHTML = '<option value="">Kategori Seçin</option>' +
        categories.map(cat => `<option value="${cat.categoryId}">${cat.categoryName}</option>`).join('');
}

async function saveCategory() {
    const token = localStorage.getItem('jewelry_token');
    
    // Verileri formdan al
    const categoryName = document.getElementById('categoryName').value;
    const categoryIcon = document.getElementById('categoryIcon').value;

    const formData = {
        categoryName: categoryName,
        categoryIcon: categoryIcon
    };

    try {
        const url = editingCategoryId 
            ? `http://localhost:5025/api/Categories/${editingCategoryId}`
            : `http://localhost:5025/api/Categories`;
        
        const method = editingCategoryId ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(formData)
        });

        if (response.ok) {
            showNotification('İşlem başarılı!', 'success');
            closeModal('categoryModal');
            await loadCategories(); // Tabloyu/Grid'i yenile
        } else {
            const errorText = await response.text();
            showNotification('Hata: ' + errorText, 'error');
        }
    } catch (error) {
        console.error("Kategori kaydetme hatası:", error);
        showNotification('Sunucu bağlantısı kurulamadı.', 'error');
    }
}

async function editCategory(id) {
    try {
        // URL'i direkt ana yoldan (api/Categories) çağırıyoruz
        const response = await fetch(`http://localhost:5025/api/Categories/${id}`);
        
        if (!response.ok) {
            throw new Error('Kategori detayları alınamadı');
        }

        const result = await response.json();
        
        // Backend ApiResponse döndürüyorsa veriyi 'result.data' içinden almalıyız
        // Eğer direkt nesne dönüyorsa 'result' kullanmalıyız
        const category = result.data || result; 
        
        editingCategoryId = id;
        
        // Form alanlarını doldurma
        document.getElementById('categoryName').value = category.categoryName;
        document.getElementById('categoryIcon').value = category.categoryIcon || 'fa-gem';
        
        // Modal başlığını güncelle ve aç
        document.querySelector('#categoryModal .modal-header h2').textContent = 'Kategoriyi Düzenle';
        openModal('categoryModal');
    } catch (error) {
        console.error('Kategori yüklenemedi:', error);
        showNotification('Kategori yüklenemedi: ' + error.message, 'error');
    }
}

async function deleteCategory(id) {
    if (!confirm('Bu kategoriyi silmek istediğinizden emin misiniz?')) return;

    try {
        // Burada da URL'i düzelttik
        const response = await fetch(`http://localhost:5025/api/Categories/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            showNotification('Kategori silindi', 'success');
            await loadCategories();
        } else {
            const error = await response.text();
            showNotification('Hata: ' + error, 'error');
        }
    } catch (error) {
        console.error('Kategori silinemedi:', error);
        showNotification('Kategori silinemedi', 'error');
    }
}
//categories end-----------------------------------------------------

// Orders ---------------------------------------------------------
async function loadOrders() {
    try {
       
        const response = await fetch(`http://localhost:5025/api/Orders/admin/all`);
        
        const result = await response.json();

        // ApiResponse yapısına göre veriyi okuyun
        if (result.success && result.data) {
            orders = result.data; // result.data direkt liste olarak gelir
            renderOrdersTable();
        }
    } catch (error) {
        console.error('Siparişler yüklenemedi:', error);
    }
}
function renderOrdersTable() {
    const tbody = document.getElementById('ordersTableBody');
    if (!tbody) return;

    if (!orders || orders.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 40px;">Henüz sipariş bulunmuyor.</td></tr>';
        return;
    }

    tbody.innerHTML = orders.map(order => `
        <tr>
            <td style="font-weight: 600; color: #8B2F39;">#${order.orderId}</td>
            <td>
                <div style="display: flex; flex-direction: column;">
                    <span style="font-weight: 500;">${order.userName}</span>
                    <small style="color: #888;">${order.userEmail || ''}</small>
                </div>
            </td>
            <td>${new Date(order.orderCreatedAt).toLocaleDateString('tr-TR')}</td>
            <td><strong style="color: #2c3e50;">₺${order.orderTotal.toFixed(2)}</strong></td>
            <td>
                <span class="status-badge ${order.orderStatus.toLowerCase()}" 
                      style="padding: 5px 10px; border-radius: 20px; font-size: 0.85rem; font-weight: 600;">
                    ${getOrderStatusText(order.orderStatus)}
                </span>
            </td>
            <td style="text-align: center; width: 80px;">
                <button class="btn-view" onclick="viewOrder(${order.orderId})" 
                        title="Detayları Görüntüle" 
                        style="background: #f1f2f6; border: none; padding: 8px; border-radius: 6px; cursor: pointer; transition: 0.3s;">
                    <i class="fas fa-eye" style="color: #6c5ce7;"></i>
                </button>
            </td>
        </tr>
    `).join('');
}

function getOrderStatusClass(status) {
    const statusMap = {
        1: 'pending',
        2: 'completed',
        3: 'cancelled'
    };
    return statusMap[status] || 'pending';
}

function getOrderStatusText(status) {
    // Backend'den gelen veriye göre (Sayı veya Metin) eşleştirme
    const statusMap = {
        // Sayısal değerler (Enum karşılıkları)
        0: 'Hazırlanıyor',
        1: 'İşleniyor',
        2: 'Kargoda',
        3: 'Teslim Edildi',
        4: 'İptal Edildi',
        
        // Metinsel değerler (Case-insensitive kontrolü için küçük harf)
        'pending': 'Hazırlanıyor',
        'processing': 'İşleniyor',
        'shipped': 'Kargoda',
        'delivered': 'Teslim Edildi',
        'cancelled': 'İptal Edildi',
        'completed': 'Tamamlandı'
    };

    // Gelen değeri stringe çevirip küçük harfle kontrol et (güvenlik için)
    const normalizedStatus = status !== null && status !== undefined ? status.toString().toLowerCase() : '';
    
    return statusMap[status] || statusMap[normalizedStatus] || 'Bilinmiyor';
}

async function viewOrder(id) {
    try {
        const response = await fetch(`http://localhost:5025/api/admin/Orders/${id}`);
        if (!response.ok) throw new Error('Order not found');

        const result = await response.json();
        const order = result.data || result;

        const customerName = order.user ? order.user.userName : 'Unknown Customer';
        const displayTotal = (order.totalAmount != null) ? order.totalAmount.toFixed(2) : "0.00";

        const detailHtml = `
            <div class="order-details">
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 20px;">
                    <div>
                        <p><strong>Order ID:</strong> #${order.orderId}</p>
                        <p><strong>Customer:</strong> ${customerName}</p>
                        <p><strong>Email:</strong> ${order.user ? order.user.userEmail : '-'}</p>
                    </div>
                    <div>
                        <p><strong>Date:</strong> ${new Date(order.orderDate).toLocaleString('tr-TR')}</p>
                        <p><strong>Total:</strong> <span style="color: #27ae60; font-weight: bold;">₺${displayTotal}</span></p>
                        <p><strong>Tracking:</strong> ${order.trackingNumber || '-'}</p>
                    </div>
                </div>

                <hr style="margin: 20px 0;">
                <h4>Products</h4>
                <table class="data-table" style="width: 100%; margin-top: 10px; border-collapse: collapse;">
                    <thead>
                        <tr style="background: #f8f9fa; text-align: left;">
                            <th style="padding: 10px; border-bottom: 2px solid #ddd;">Product Name</th>
                            <th style="padding: 10px; border-bottom: 2px solid #ddd;">Quantity</th>
                            <th style="padding: 10px; border-bottom: 2px solid #ddd;">Price</th>
                        </tr>
                    </thead>
<tbody>
    ${order.orderItems && order.orderItems.length > 0 ? 
        order.orderItems.map(item => {
            const itemPrice =  item.unitPrice;
            return `
                <tr>
                    <td style="padding: 10px; border-bottom: 1px solid #eee;">${item.productName || 'Product'}</td>
                    <td style="padding: 10px; border-bottom: 1px solid #eee;">${item.quantity}</td>
                    <td style="padding: 10px; border-bottom: 1px solid #eee;">₺${itemPrice.toFixed(2)}</td>
                </tr>
            `;
        }).join('') 
        : '<tr><td colspan="3" style="text-align:center; padding:20px;">No products found.</td></tr>'
    }
</tbody>
                </table>

                <hr style="margin: 20px 0;">
                <div class="status-update-box" style="background: #f1f2f6; padding: 15px; border-radius: 8px;">
                    <label style="display: block; margin-bottom: 10px; font-weight: bold;">Update Order Status:</label>
                    <div style="display: flex; gap: 10px;">
                        <select id="newStatusSelect" class="form-control" style="flex: 1; padding: 8px; border-radius: 4px; border: 1px solid #ddd;">
                            <option value="0" ${order.orderStatus == "0" ? 'selected' : ''}>Pending</option>
                            <option value="1" ${order.orderStatus == "1" ? 'selected' : ''}>Processing</option>
                            <option value="2" ${order.orderStatus == "2" ? 'selected' : ''}>Shipped</option>
                            <option value="3" ${order.orderStatus == "3" ? 'selected' : ''}>Delivered</option>
                            <option value="4" ${order.orderStatus == "4" ? 'selected' : ''}>Cancelled</option>
                        </select>
                        <button onclick="updateOrderStatus(${id})" class="btn-update" style="background: #6c5ce7; color: white; border: none; padding: 8px 20px; border-radius: 4px; cursor: pointer; font-weight: bold;">
                            Update
                        </button>
                    </div>
                </div>
            </div>
        `;
        document.getElementById('orderModalBody').innerHTML = detailHtml;
        openModal('orderModal');
    } catch (error) {
        console.error('Failed to load order:', error);
        showNotification('Failed to load order details.', 'error');
    }
}
async function updateOrderStatus(orderId) {
    const statusSelect = document.getElementById('newStatusSelect');
    const statusText = statusSelect.options[statusSelect.selectedIndex].text;

    try {
        const response = await fetch(`http://localhost:5025/api/admin/Orders/${orderId}/status`, {
            method: 'PUT',
            headers: { 
                'Content-Type': 'application/json' 
            },
            body: JSON.stringify({
                newStatus: statusText,
                notes: "Status updated by Admin "
            })
        });

        if (response.ok) {
            showNotification('Order status updated successfully!', 'success');
            closeModal('orderModal');
            loadOrders(); 
        } else {
            const errorText = await response.text();
            showNotification('Update failed: ' + errorText, 'error');
        }
    } catch (error) {
        console.error('Update status error:', error);
        showNotification('System error during update.', 'error');
    }
}
