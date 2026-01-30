// API Base URL
const API_BASE_URL = 'http://localhost:5025/api';

// Global değişkenler
let currentUser = null;
let userOrders = [];
let userFavorites = [];
let userAddresses = [];

let editingAddressId = null; 

let authToken = null;


document.addEventListener('DOMContentLoaded', function() {
    console.log('Sayfa yüklendi, kontroller başlatılıyor...');
    
    if (!checkLogin()) {
        return; // Login kontrolü başarısız olduysa devam etme
    }
    
    setupNavigation();
    setupModals();
    setupForms();
    
    // Verileri yükle
    loadUserData();
});


async function changePassword() {
    const oldPassword = document.getElementById('oldPassword').value;
    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;

    if (newPassword !== confirmPassword) {
        showNotification('Yeni şifreler eşleşmiyor', 'error');
        return;
    }

    try {
        showLoading('Şifre değiştiriliyor...');
        
        await apiRequest('/Auth/ChangePassword', {
            method: 'PUT',
            body: JSON.stringify({
                oldPassword: oldPassword,
                newPassword: newPassword
            })
        });

        showNotification('Şifreniz başarıyla değiştirildi', 'success');
        document.getElementById('passwordForm').reset(); // Formu temizle
        
    } catch (error) {
        console.error('Şifre hatası:', error);
        showNotification('Mevcut şifre hatalı veya işlem başarısız', 'error');
    } finally {
        hideLoading();
    }
}

async function updateProfile() {
    const userName = document.getElementById('updateUserName').value;
    const userPhone = document.getElementById('updateUserPhone').value;

    try {
        showLoading('Bilgiler güncelleniyor...');
        
        const response = await apiRequest('/Auth/Profile', {
            method: 'PUT',
            body: JSON.stringify({
                userName: userName,
                userPhone: userPhone
            })
        });

        showNotification('Profil bilgileriniz başarıyla güncellendi', 'success');
        
        // LocalStorage'daki kullanıcı adını da güncelle ki header'da değişsin
        let user = JSON.parse(localStorage.getItem('currentUser'));
        user.fullName = userName;
        localStorage.setItem('currentUser', JSON.stringify(user));
        
        // Sayfadaki bilgileri tazele
        displayUserInfo();
        
    } catch (error) {
        console.error('Güncelleme hatası:', error);
        showNotification('Bilgiler güncellenemedi', 'error');
    } finally {
        hideLoading();
    }
}
function checkLogin() {
    try {
        const userData = localStorage.getItem('currentUser');
        const token = localStorage.getItem('jewelry_token') || localStorage.getItem('authToken');
        
        // EĞER KULLANICI YOKSA
        if (!userData) {
            // Sadece profil sayfasındaysak Login'e yönlendir
            if (window.location.pathname.includes('profile.html')) {
                console.warn('❌ Profil sayfası için giriş gerekli. Yönlendiriliyor...');
                window.location.href = 'Login.html';
                return false;
            }
            // Kategori veya Ana sayfadaysak sadece sessizce 'false' dön, yönlendirme yapma!
            console.log('ℹ️ Ziyaretçi modu: Giriş yapılmamış.');
            return false;
        }
        
        // Kullanıcı varsa verileri işle
        currentUser = JSON.parse(userData);
        authToken = token ? token : null;

        // ID ve Email eşitlemeleri (senin mevcut mantığın)
        if (!currentUser.id && currentUser.userId) currentUser.id = currentUser.userId;
        if (!currentUser.email && currentUser.userEmail) currentUser.email = currentUser.userEmail;
        
        // Header'daki Login -> Profilim değişimini yap
        if (typeof displayUserInfo === 'function') {
            displayUserInfo();
        }
        
        return true;
    } catch (e) {
        console.error('❌ Login kontrolü hatası:', e);
        // Hata durumunda da sadece profil sayfasındaysak yönlendir
        if (window.location.pathname.includes('profile.html')) {
            window.location.href = 'Login.html';
        }
        return false;
    }
}

async function loadUserData() {
    console.log('Kullanıcı verileri yükleniyor...');
    try {
        showLoading('Veriler yükleniyor...');
        
        // Siparişleri yükle
        try {
            const response = await apiRequest('/Orders/my');
            // Eğer response.data varsa onu al, yoksa response'un kendisini al (Eğer dizi değilse boş dizi yap)
            userOrders = response.data || (Array.isArray(response) ? response : []);
            console.log('Siparişler yüklendi:',userOrders[0]);
        } catch (error) {
            console.error('Sipariş yükleme hatası:', error);
            userOrders = [];
        }
        
        // Favorileri yükle
      try {
    const response = await apiRequest('/Favorites');
    // Controller doğrudan List<Favorite> döndürdüğü için dizi kontrolü yapıyoruz
    userFavorites = Array.isArray(response) ? response : (response.data || []);
    console.log('Favoriler yüklendi:', userFavorites.length);
} catch (error) {
    console.error('Favori yükleme hatası:', error);
    userFavorites = [];
}
        
        // Adresleri yükle
        try {
            const response = await apiRequest(`/Addresses/user/${currentUser.id}`);
            userAddresses = response.data || (Array.isArray(response) ? response : []);
            console.log('Adresler yüklendi:', userAddresses.length);
        } catch (error) {
            console.error('Adres yükleme hatası:', error);
            userAddresses = [];
        }

        displayOrders();
        displayFavorites();
        displayAddresses();
        
        hideLoading();
    } catch (error) {
        console.error('Veri yükleme genel hatası:', error);
        hideLoading();
        showNotification('Veriler yüklenirken hata oluştu', 'error');
    }
}
// Navigasyon setup
function setupNavigation() {
    const navLinks = document.querySelectorAll('.nav-link');
    const sections = document.querySelectorAll('.section');

    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const sectionId = this.getAttribute('data-section');

            navLinks.forEach(l => l.classList.remove('active'));
            this.classList.add('active');

            sections.forEach(section => {
                section.classList.remove('active');
            });
            document.getElementById(sectionId).classList.add('active');
        });
    });

    // Çıkış butonu
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', logout);
    }
}

// Çıkış yap
function logout() {
    if (confirm('Çıkış yapmak istediğinizden emin misiniz?')) {
        // Tüm login verilerini temizle
        localStorage.removeItem('currentUser');
        localStorage.removeItem('jewelry_token');
        localStorage.removeItem('authToken');
        showNotification('Çıkış yapılıyor...', 'info');
        setTimeout(() => {
            window.location.href = 'index.html';
        }, 1000);
    }
}

// Modal setup

function setupModals() {
    const addressModal = document.getElementById('addressModal');
    const addAddressBtn = document.getElementById('addAddressBtn'); // Hata veren yer burası
    const closeButtons = document.querySelectorAll('.close-modal');

    // Sadece buton sayfada varsa event ekle
    if (addAddressBtn) {
        addAddressBtn.addEventListener('click', function() {
            editingAddressId = null;
            const form = document.getElementById('addressForm');
            if (form) form.reset();
            addressModal.classList.add('active');
        });
    }

    if (closeButtons.length > 0) {
        closeButtons.forEach(btn => {
            btn.addEventListener('click', function() {
                if (addressModal) addressModal.classList.remove('active');
            });
        });
    }
}

// Form setup
function setupForms() {
    const addressForm = document.getElementById('addressForm');
    
    if (addressForm) {
        addressForm.addEventListener('submit', function(e) {
            e.preventDefault();
            saveAddress();
        });
    }
}

// Kullanıcı bilgilerini göster
function displayUserInfo() {
    const userName = document.getElementById('userName');
    const userEmail = document.getElementById('userEmail');
    
    if (userName && currentUser) {
        userName.textContent = currentUser.fullName || currentUser.name || 'Kullanıcı';
    }
    if (userEmail && currentUser) {
        userEmail.textContent = currentUser.email || '';
    }
}

function displayOrders() {
    const container = document.getElementById('ordersContainer');
    if (!container) return;
    
    container.innerHTML = '';
    
    if (!userOrders || userOrders.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <i class="fas fa-box-open"></i>
                <h3>Henüz Siparişiniz Yok</h3>
                <p>Alışverişe başlamak için ürünlerimize göz atın!</p>
            </div>`;
        return;
    }

    userOrders.forEach(order => {
        // API'den gelen verileri güvenli bir şekilde alıyoruz
        const id = order.orderId || order.id;
        const total = order.orderTotal || order.totalAmount || 0;
        const status = order.orderStatus || order.status || 'İşleniyor';
        const date = order.orderCreatedAt || order.createdAt;
        const items = order.items || []; // Sipariş içindeki ürün listesi

        const orderCard = document.createElement('div');
        orderCard.className = 'order-card';
        
        // Ürün listesini HTML olarak hazırla
        const itemsHtml = items.length > 0 ? items.map(item => `
            <div class="order-product-item">
                <img src="images/${item.productImage}" 
                     alt="${item.productName}">
                <div class="product-info">
                    <span class="product-name">${item.productName || 'Ürün Bilgisi Yok'}</span>
                    <span class="product-qty">Adet: ${item.quantity} x ${parseFloat(item.price || 0).toFixed(2)} ₺</span>
                </div>
            </div>
        `).join('') : '<p style="padding: 20px; color: #999;">Sipariş içeriği yüklenemedi.</p>';

        orderCard.innerHTML = `
            <div class="order-card-header">
                <div class="header-left">
                    <span class="order-no">Sipariş #${id}</span>
                    <span class="order-date"><i class="fas fa-calendar-alt"></i> ${formatDate(date)}</span>
                </div>
                <div class="header-right">
                    <span class="status-badge ${status.toLowerCase()}">${getOrderStatusText(status)}</span>
                </div>
            </div>
            
            <div class="order-card-content">
                <div class="product-list">
                    ${itemsHtml}
                </div>
            </div>
            
            <div class="order-card-footer">
                <div class="footer-summary">
                    <span class="total-label" style="color: #666; font-weight: 500;">Toplam Tutar:</span>
                    <span class="total-price">${parseFloat(total).toFixed(2)} ₺</span>
                </div>
            
            </div>
        `;
        container.appendChild(orderCard);
    });
}
// Sipariş durumu metni
function getOrderStatusText(status) {
    const statusMap = {
        'Pending': 'Hazırlanıyor',
        'Processing': 'İşleniyor',
        'Shipped': 'Kargoda',
        'Delivered': 'Teslim Edildi',
        'Cancelled': 'İptal Edildi',
        'pending': 'Hazırlanıyor',
        'completed': 'Tamamlandı',
        'cancelled': 'İptal Edildi'
    };
    return statusMap[status] || status;
}

// Tarih formatla
function formatDate(dateString) {
    if (!dateString) return 'Tarih belirtilmemiş';
    try {
        const date = new Date(dateString);
        return date.toLocaleDateString('tr-TR', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    } catch (e) {
        return dateString;
    }
}

function displayFavorites() {
    const container = document.getElementById('favoritesContainer');
    if (!container) return;
    
    container.innerHTML = '';
    
    if (!userFavorites || userFavorites.length === 0) {
        container.innerHTML = `<div class="empty-state"><h3>Favori ürününüz yok.</h3></div>`;
        return;
    }

    userFavorites.forEach(favorite => {
        const product = favorite.product;
        if (!product) return;

        const resimAdi = product.productImage || 'logo.png'; 
        const finalImgPath = `images/${resimAdi}`;
        const fiyat = product.productPrice || 0;

        const card = document.createElement('div');
        card.className = 'favorite-card';
        // Kartın kendisine tıklama özelliği ekliyoruz
        card.style.cursor = 'pointer'; 
        
        card.innerHTML = `
            <button class="remove-favorite" title="Favorilerden Kaldır">
                <i class="fas fa-times"></i>
            </button>
            <img src="${finalImgPath}" alt="${product.productName}" onerror="this.src='images/logo.png'">
            <h3>${product.productName}</h3>
            <p class="price" style="color: #27ae60; font-weight: 700;">${parseFloat(fiyat).toFixed(2)} ₺</p>
        `;

        // --- TIKLAMA OLAYLARI ---

        // 1. Silme butonuna tıklandığında (Karta sıçramasını engellemek için stopPropagation kullanıyoruz)
        const deleteBtn = card.querySelector('.remove-favorite');
        deleteBtn.addEventListener('click', (e) => {
            e.stopPropagation(); // Karta tıklama olayını tetiklemez
            removeFavorite(favorite.favoriteId);
        });

        // 2. Kartın herhangi bir yerine (buton hariç) tıklandığında detay sayfasına git
        card.addEventListener('click', () => {
            const productId = product.productId || product.id;
            if (productId) {
                window.location.href = `product-detail.html?id=${productId}`;
            }
        });

        container.appendChild(card);
    });
}
async function removeFavorite(favoriteId) {
    if (!confirm('Bu ürünü favorilerinizden çıkarmak istediğinizden emin misiniz?')) return;
    
    try {
        showLoading('Favori siliniyor...');
        
        await apiRequest(`/Favorites/${favoriteId}`, { 
            method: 'DELETE' 
        });
        
       
        userFavorites = userFavorites.filter(f => f.favoriteId !== favoriteId);
        displayFavorites();
        showNotification('Ürün favorilerden çıkarıldı', 'success');
    } catch (error) {
        console.error('Favori silme hatası:', error);
    } finally {
        hideLoading();
    }
}

function displayAddresses() {
    const container = document.getElementById('addressesContainer');
    if (!container) return;
    console.log(userAddresses[0],userAddresses[1])
    container.innerHTML = '';
    
    if (!userAddresses || userAddresses.length === 0) {
        container.innerHTML = '<div class="empty-state"><h3>Kayıtlı adres bulunamadı.</h3></div>';
        return;
    }

    userAddresses.forEach(address => {
        // API'den gelen gerçek isimleri eşliyoruz
        const id = address.addressId || address.id;
        const title = address.addressTitle || "Başlıksız Adres";
        const name = address.recipientName || "İsim Belirtilmemiş";
        const phone = address.recipientPhone || "";
        const detail = address.addressDetail || "";
        const city = address.city || "";
        const district = address.district || "";

        const card = document.createElement('div');
        card.className = 'address-card' + (address.isDefault ? ' default' : '');
        card.innerHTML = `
            <div class="address-header">
                <h3 class="address-title">${title}</h3>
                ${address.isDefault ? '<span class="badge">Varsayılan</span>' : ''}
            </div>
            <div class="address-details">
                <strong>${name}</strong><br>
                ${phone}<br>
                ${detail}<br>
                ${district} / ${city}
            </div>
            <div class="address-actions">
                <button class="btn-secondary" onclick="editAddress(${id})">
                    <i class="fas fa-edit"></i> Düzenle
                </button>
                <button class="btn-danger" onclick="deleteAddress(${id})">
                    <i class="fas fa-trash"></i> Sil
                </button>
            </div>
        `;
        container.appendChild(card);
    });
}
async function deleteAddress(id) {
    if (!confirm('Bu adresi silmek istediğinizden emin misiniz?')) return;
    try {
        showLoading('Adres siliniyor...');
        // URL yapısını Swagger'daki gibi güncelleyin: /api/Addresses/{id}?userId={userId}
        await apiRequest(`/Addresses/${id}?userId=${currentUser.userId || currentUser.id}`, { 
            method: 'DELETE' 
        });
        userAddresses = userAddresses.filter(a => (a.addressId || a.id) !== id);
        displayAddresses();
        showNotification('Adres silindi', 'success');
    } catch (error) {
        console.error('Silme hatası:', error);
    } finally {
        hideLoading();
    }
}
function editAddress(id) {
    // API'den gelen addressId ile eşleşen kaydı bul
    const address = userAddresses.find(a => (a.addressId || a.id) === id);
    
    if (address) {
        editingAddressId = id; // Global ID'yi güncelle
        
        // Formu doldururken API'nin isimlendirmelerini kullan
        document.getElementById('addressTitle').value = address.addressTitle || '';
        document.getElementById('addressName').value = address.recipientName || '';
        document.getElementById('addressPhone').value = address.recipientPhone || '';
        document.getElementById('addressCity').value = address.city || '';
        document.getElementById('addressDistrict').value = address.district || '';
        document.getElementById('addressDetail').value = address.addressDetail || '';
        
        document.getElementById('addressModal').classList.add('active');
    }
}
async function saveAddress() {
    // 1. Form elemanlarını yakala (ID'lerin doğruluğundan emin olun)
    const titleElem = document.getElementById('addressTitle');
    const nameElem = document.getElementById('addressName');
    const phoneElem = document.getElementById('addressPhone');
    const cityElem = document.getElementById('addressCity');
    const districtElem = document.getElementById('addressDistrict');
    const detailElem = document.getElementById('addressDetail');

    // 2. Değerleri al ve boşlukları temizle
    const title = titleElem ? titleElem.value.trim() : "";
    const name = nameElem ? nameElem.value.trim() : "";
    const phone = phoneElem ? phoneElem.value.trim() : "";
    const city = cityElem ? cityElem.value.trim() : "";
    const district = districtElem ? districtElem.value.trim() : "";
    const detail = detailElem ? detailElem.value.trim() : "";

    // 3. Frontend Doğrulaması: Boş alan kontrolü
    if (!title || !name || !phone || !city || !district || !detail) {
        showNotification('Lütfen tüm zorunlu alanları doldurun.', 'error');
        console.error("Eksik Alanlar:", { title, name, phone, city, district, detail });
        return;
    }

    try {
        showLoading('İşlem yapılıyor...');
        
        if (editingAddressId) {
            // --- GÜNCELLEME (PUT) ---
            // Orijinal adresi bul (isDefault gibi değişmemesi gereken alanları korumak için)
            const originalAddress = userAddresses.find(a => (a.addressId || a.id) == editingAddressId);
            
            const updatedPayload = {
                ...originalAddress, 
                addressId: parseInt(editingAddressId),
                addressTitle: title,
                recipientName: name,
                recipientPhone: phone,
                city: city,
                country:"Türkiye",
                district: district,
                addressDetail: detail,
                userId: parseInt(currentUser.userId || currentUser.id)
            };

            console.log("🚀 PUT Payload gönderiliyor:", updatedPayload);

            await apiRequest(`/Addresses/${editingAddressId}`, {
                method: 'PUT',
                body: JSON.stringify(updatedPayload)
            });

            showNotification('Adres başarıyla güncellendi', 'success');
        } else {
            // --- YENİ KAYIT (POST) ---
            const newAddress = {
                addressTitle: title,
                recipientName: name,
                recipientPhone: phone,
                city: city,
                district: district,
    
                addressDetail: detail,
                userId: parseInt(currentUser.userId || currentUser.id)
            };

            await apiRequest('/Addresses', {
                method: 'POST',
                body: JSON.stringify(newAddress)
            });

            showNotification('Adres başarıyla eklendi', 'success');
        }

        // Başarılı işlem sonrası temizlik
        editingAddressId = null;
        document.getElementById('addressForm').reset();
        document.getElementById('addressModal').classList.remove('active');
        await loadUserData(); // Listeyi yenile

    } catch (error) {
        console.error("🚨 Adres İşlem Hatası:", error);
        showNotification('Hata: ' + error.message, 'error');
    } finally {
        hideLoading();
    }
}
// Varsayılan adres yap
async function setDefaultAddress(id) {
    try {
        showLoading('Varsayılan adres güncelleniyor...');
        await apiRequest(`/Addresses/${id}/set-default`, {
            method: 'PUT'
        });
        
        // Local verileri güncelle
        userAddresses.forEach(address => {
            address.isDefault = address.id === id;
        });
        
        displayAddresses();
        showNotification('Varsayılan adres güncellendi', 'success');
        hideLoading();
    } catch (error) {
        hideLoading();
        console.error('Varsayılan adres ayarlama hatası:', error);
        showNotification('İşlem başarısız: ' + error.message, 'error');
    }
}

// Loading göster
function showLoading(message = 'Yükleniyor...') {
    let loader = document.getElementById('globalLoader');
    if (!loader) {
        loader = document.createElement('div');
        loader.id = 'globalLoader';
        loader.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0,0,0,0.7);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 9999;
        `;
        loader.innerHTML = `
            <div style="background: white; padding: 30px; border-radius: 10px; text-align: center; min-width: 200px;">
                <div class="spinner" style="border: 4px solid #f3f3f3; border-top: 4px solid #667eea; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin: 0 auto 15px;"></div>
                <p id="loadingMessage" style="margin: 0; color: #333; font-weight: 600;">${message}</p>
            </div>
        `;
        document.body.appendChild(loader);
        
        // Spinner animasyonu
        if (!document.getElementById('spinnerStyle')) {
            const style = document.createElement('style');
            style.id = 'spinnerStyle';
            style.textContent = `
                @keyframes spin {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }
            `;
            document.head.appendChild(style);
        }
    } else {
        const messageEl = loader.querySelector('#loadingMessage');
        if (messageEl) {
            messageEl.textContent = message;
        }
    }
    loader.style.display = 'flex';
}

// Loading gizle
function hideLoading() {
    const loader = document.getElementById('globalLoader');
    if (loader) {
        loader.style.display = 'none';
    }
}

// Bildirim göster
function showNotification(message, type = 'info') {
    console.log(`[${type.toUpperCase()}] ${message}`);
    
    const notification = document.createElement('div');
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: white;
        padding: 15px 25px;
        border-radius: 8px;
        box-shadow: 0 5px 20px rgba(0,0,0,0.3);
        display: flex;
        align-items: center;
        gap: 12px;
        z-index: 10000;
        animation: slideIn 0.3s ease;
        border-left: 4px solid ${type === 'success' ? '#27ae60' : type === 'error' ? '#e74c3c' : '#3498db'};
        max-width: 400px;
    `;
    
    const iconMap = {
        success: 'check-circle',
        error: 'exclamation-circle',
        info: 'info-circle'
    };
    
    const colorMap = {
        success: '#27ae60',
        error: '#e74c3c',
        info: '#3498db'
    };
    
    notification.innerHTML = `
        <i class="fas fa-${iconMap[type]}" 
           style="color: ${colorMap[type]}; font-size: 20px;"></i>
        <span style="color: #333;">${message}</span>
    `;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => notification.remove(), 300);
    }, 5000);
}

// CSS animasyonları
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from { transform: translateX(100px); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    @keyframes slideOut {
        from { transform: translateX(0); opacity: 1; }
        to { transform: translateX(100px); opacity: 0; }
    }
`;
document.head.appendChild(style);