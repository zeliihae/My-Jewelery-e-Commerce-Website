
const API_BASE_URL = 'http://localhost:5025/api'; 


let filteredProducts = [];
let currentCategoryName = ''; 

document.addEventListener('DOMContentLoaded', async function() {
    const urlParams = new URLSearchParams(window.location.search);
    currentCategoryName = urlParams.get('cat') || '';

    // 1. Sepet sayısını ve Kullanıcı bilgisini (Header) hemen güncelle
    if (typeof updateCartCount === 'function') updateCartCount();
    if (typeof displayUserInfo === 'function') displayUserInfo(); // script.js'deki fonksiyonu çağırır

    // 2. Kategorileri ve Navbar'ı Yükle
    await loadDynamicCategories();
    
    // 3. Ürünleri API'dan çek
    await loadProducts();
    
    // Filtreleri kur
    if (typeof setupFilters === 'function') setupFilters(); 
});

// Kategori sayfasındaki Login/Signup durumunu kontrol eden fonksiyon
function checkLoginStatus() {
    const token = localStorage.getItem('jewelry_token');
    const currentUser = localStorage.getItem('currentUser');
    
    // HTML'deki elementleri seç (Sizin HTML yapınıza göre)
    const loginLink = document.querySelector('a[href="Login.html"]');
    const signupLink = document.querySelector('a[href="signup.html"]');
    const profileLink = document.querySelector('.nav-link[href="profile.html"]');

    if (token && currentUser) {
        const user = JSON.parse(currentUser);
        if (loginLink) {
            loginLink.href = "profile.html";
            loginLink.innerHTML = `<i class="fas fa-user-circle"></i> ${user.fullName || user.name}`;
            loginLink.style.display = "inline-block"; // Görünür yap
        }
        if (signupLink) signupLink.style.display = "none"; // Signup'ı gizle
        if (profileLink) profileLink.style.display = "none"; // Fazladan profilim linki varsa gizle (Login linki profile dönüştü)
    }
}



// category.js dosyasına eklenecek arama kodları

// 1. Arama kutusunu ve ikonunu seç
const categorySearchBox = document.querySelector("#search-box");
const categorySearchIcon = document.querySelector(".search-form i");


// 2. Arama Tetikleyicileri
if (categorySearchBox) {
    categorySearchBox.addEventListener("keypress", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            const term = e.target.value.trim();
            if (term.length >= 2) {
                searchInDatabase(term);
                // Arama formunu kapat (mobil uyum için)
                const searchForm = document.querySelector(".search-form");
                if (searchForm) searchForm.classList.remove("active");
            }
        }
    });
}

if (categorySearchIcon) {
    categorySearchIcon.addEventListener("click", function () {
        const term = categorySearchBox.value.trim();
        if (term) searchInDatabase(term);
    });
}
function displayProducts() {
    const container = document.getElementById('productsContainer');
    if (!container) return;
    container.innerHTML = '';
    
    document.getElementById('productCount').textContent = `${filteredProducts.length} Ürün`;

    if (filteredProducts.length === 0) {
        document.getElementById('noProducts').style.display = 'block';
        return;
    }

    document.getElementById('noProducts').style.display = 'none';

    filteredProducts.forEach(product => {
        const card = document.createElement('div');
        card.className = 'product-card';
        card.setAttribute('data-product-id', product.id); 

        const displayPrice = product.discountPrice || product.price;
        const finalImgPath = `images/${product.imageName}`;
        
        // KARTIN İÇERİĞİ
        card.innerHTML = `
            <div class="product-image">
                <button class="favorite-btn" onclick="handleFavoriteClick(event, ${product.id})">
                    <i class="far fa-heart"></i>
                </button>
                <img src="${finalImgPath}" alt="${product.name}" onerror="this.src='images/logo.png'">
            </div>
            <div class="product-details">
                <h3 class="product-name">${product.name}</h3>
                <div class="product-price">
                    <span class="current-price">${displayPrice.toFixed(2)} ₺</span>
                </div>
              <button class="btn-add-cart" onclick="addToCartFromCategory(event, ${product.id})">
    <i class="fas fa-shopping-cart"></i> Add to Cart
</button>
            </div>
        `;

        // SORUN 1 ÇÖZÜM: Kartın kendisine tıklanınca detaya git
        card.addEventListener('click', (e) => {
            // Eğer tıklanan şey favori butonu veya sepete ekle butonu değilse yönlendir
            if (!e.target.closest('.favorite-btn') && !e.target.closest('.btn-add-cart')) {
                window.location.href = `product-detail.html?id=${product.id}`;
            }
        });

        container.appendChild(card);
        updateFavoriteButtonState(product.id, card);
    });
}

// 3. API'den Arama Yapan Fonksiyon
async function searchInDatabase(searchTerm) {
    const container = document.getElementById('productsContainer');
    const titleElement = document.getElementById('categoryTitle');
    const descElement = document.getElementById('categoryDescription');
    const countElement = document.getElementById('productCount');

    try {
        // Yükleniyor durumu
        container.innerHTML = '<div style="grid-column: 1/-1; text-align:center; padding:50px;"><i class="fas fa-spinner fa-spin" style="font-size:3rem;"></i><p>Aranıyor...</p></div>';

        // Backend API'sine istek (Public Products endpoint)
        const response = await fetch(`${API_BASE_URL}/Products?search=${encodeURIComponent(searchTerm)}`);
        const result = await response.json();

        // Ürünleri al
        const products = result.data || result;

        // UI Güncelleme
        titleElement.innerHTML = `ARAMA: <span>"${searchTerm}"</span>`;
        descElement.textContent = "Arama kriterlerinize uygun sonuçlar listeleniyor.";
        
        if (!products || products.length === 0) {
            countElement.textContent = "0 Ürün";
            document.getElementById('noProducts').style.display = 'block';
            container.innerHTML = '';
            return;
        }

        document.getElementById('noProducts').style.display = 'none';
        countElement.textContent = `${products.length} Ürün Bulundu`;

        // Gelen ürünleri global değişkenlere ata (filtreleme vs. çalışmaya devam etsin diye)
        allProducts = products.map(p => ({
            ...p,
            id: p.productId,
            name: p.productName,
            price: p.productPrice,
            discountPrice: p.productDiscountPrice,
            imageName: p.productImage || 'logo.png',
            category: p.categoryName || ""
        }));

        filteredProducts = [...allProducts];
        
        // Ekrana bas
        displayProducts();

        // Arama sonuçlarına yumuşak kaydır
        container.scrollIntoView({ behavior: 'smooth', block: 'start' });

    } catch (error) {
        console.error("Arama hatası:", error);
        showNotification('Arama sırasında bir hata oluştu.', 'error');
    }
}
async function loadDynamicCategories() {
    try {
        const response = await fetch(`${API_BASE_URL}/Categories`);
        const result = await response.json();

        if (result.success && result.data) {
            const navbar2 = document.querySelector('.navbar2');
            if (!navbar2) return;

            // "Tüm Ürünler" butonunu ekle
            let menuHtml = `<a href="category.html?cat=Tüm Ürünler" class="${currentCategoryName === 'Tüm Ürünler' || !currentCategoryName ? 'active' : ''}">Tüm Ürünler <i class="fa-solid fa-gem"></i></a>`;

            result.data.forEach(cat => {
                const isActive = cat.categoryName === currentCategoryName ? 'active' : '';
                menuHtml += `<a href="category.html?cat=${encodeURIComponent(cat.categoryName)}" class="${isActive}">
                                ${cat.categoryName} <i class="fa-solid fa-angle-down"></i>
                             </a>`;
            });
            navbar2.innerHTML = menuHtml;

            // Sayfa başlığını güncelle
            const activeCat = result.data.find(c => c.categoryName === currentCategoryName);
            if (activeCat) {
                updateCategoryUI(activeCat);
            } else {
                updateCategoryUI({ categoryName: 'Tüm Ürünler', categoryDescription: 'En Özel Koleksiyonlarımızı Keşfedin' });
            }
        }
    } catch (error) {
        console.error("Kategoriler yüklenemedi:", error);
    }
}

async function loadProducts() {
    try {
       
        const response = await fetch(`${API_BASE_URL}/admin/Products`);
        const result = await response.json();

   
        let rawProducts = result.data?.products || result.products || [];

        if (rawProducts.length > 0) {
            allProducts = rawProducts.map(p => ({
                ...p,
                id: p.productId,
                name: p.productName,
                price: p.productPrice,
                discountPrice: p.productDiscountPrice,
                imageName: p.productImage || 'logo.png',
                
                category: p.categoryName || p.category?.categoryName || ""
            }));
            
            filterProducts(); 
        }
    } catch (error) {
        console.error("Ürün yükleme hatası:", error);
    }
}

// Olay Dinleyicilerini Başlat
function setupFilters() {
    const sortSelect = document.getElementById('sortSelect');
    const minPriceInput = document.getElementById('minPrice');
    const maxPriceInput = document.getElementById('maxPrice');
    const applyFilterBtn = document.getElementById('applyFilter');

    // Sıralama değiştiğinde anında filtrele
    if (sortSelect) {
        sortSelect.addEventListener('change', filterProducts);
    }

    // "Uygula" butonuna basıldığında fiyatları filtrele
    if (applyFilterBtn) {
        applyFilterBtn.addEventListener('click', filterProducts);
    }

    // Enter tuşuna basıldığında da fiyat filtresini çalıştır
    [minPriceInput, maxPriceInput].forEach(input => {
        if (input) {
            input.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') filterProducts();
            });
        }
    });
}

function filterProducts() {
    // 1. Adım: Kategoriye Göre Filtrele
    let results = [];
    if (!currentCategoryName || currentCategoryName === 'Tüm Ürünler') {
        results = [...allProducts];
    } else {
        results = allProducts.filter(p => 
            p.category.toLowerCase().trim() === currentCategoryName.toLowerCase().trim()
        );
    }

    // 2. Adım: Fiyat Aralığına Göre Filtrele
    const minP = parseFloat(document.getElementById('minPrice').value) || 0;
    const maxP = parseFloat(document.getElementById('maxPrice').value) || Infinity;

    results = results.filter(p => {
        const actualPrice = p.discountPrice || p.price;
        return actualPrice >= minP && actualPrice <= maxP;
    });

    // 3. Adım: Sıralama Mantığı
    const sortVal = document.getElementById('sortSelect').value;
    if (sortVal === 'price-asc') {
        results.sort((a, b) => (a.discountPrice || a.price) - (b.discountPrice || b.price));
    } else if (sortVal === 'price-desc') {
        results.sort((a, b) => (b.discountPrice || b.price) - (a.discountPrice || a.price));
    } else if (sortVal === 'name-asc') {
        results.sort((a, b) => a.name.localeCompare(b.name));
    } else if (sortVal === 'name-desc') {
        results.sort((a, b) => b.name.localeCompare(a.name));
    }

    // Global değişkeni güncelle ve ekrana bas
    filteredProducts = results;
    displayProducts();
}


function addFavoriteButtonToCard(card) {
    if (card.querySelector('.favorite-btn')) return;
    
    const favoriteBtn = document.createElement('button');
    favoriteBtn.className = 'favorite-btn';
    favoriteBtn.innerHTML = '<i class="far fa-heart"></i>';
    favoriteBtn.title = 'Favorilere Ekle';
    
    favoriteBtn.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        // script.js içinde tanımladığımız toggleFavorite fonksiyonunu kullanacak
        toggleFavorite(card);
    });
    
    card.style.position = 'relative';
    card.insertBefore(favoriteBtn, card.firstChild);
    
    // Veritabanı durumuna göre kalbi güncelle
    updateFavoriteButton(card);
}


async function handleFavoriteClick(event, productId) {
    event.preventDefault();
    event.stopPropagation();
    
    // Tıklanan butonu güvenli bir şekilde yakala
    const btn = event.currentTarget || event.target.closest('.favorite-btn');
    if (!btn) return;

    try {
        // script.js içindeki veritabanı fonksiyonunu çağırır
        const result = await toggleProductFavorite(productId); 
        
        const icon = btn.querySelector('i');
        if (icon) {
            if (result) {
                icon.className = 'fas fa-heart'; // Dolu kalp
                btn.classList.add('active');
            } else {
                icon.className = 'far fa-heart'; // Boş kalp
                btn.classList.remove('active');
            }
        }
    } catch (error) {
        console.error("Favori işlemi hatası:", error); //
    }
}

async function updateFavoriteButtonState(productId, cardElement) {
    // 1. Kontrol: apiRequest fonksiyonu mevcut mu?
    if (typeof apiRequest !== 'function') {
        console.warn("apiRequest fonksiyonu bulunamadı. Lütfen profile.js dosyasının yüklendiğinden emin olun.");
        return;
    }

    const token = localStorage.getItem('jewelry_token');
    if (!token) return; // Giriş yapılmadıysa sorgulama yapma

    try {
        const isFavorite = await apiRequest(`/Favorites/check/${productId}`);
        const btn = cardElement.querySelector('.favorite-btn');
        if (btn) {
            const icon = btn.querySelector('i');
            if (isFavorite && icon) {
                icon.className = 'fas fa-heart';
                btn.classList.add('active');
            }
        }
    } catch (error) {
        console.error(`${productId} favori kontrol hatası:`, error);
    }
}

function updateCategoryUI(data) {
    const title = document.getElementById('categoryTitle');
    const desc = document.getElementById('categoryDescription');
    if (title) title.textContent = data.categoryName;
    if (desc) desc.textContent = data.categoryDescription || "En Özel Koleksiyonlarımızı Keşfedin";
}
// category.js içindeki fonksiyonu bu şekilde değiştirin
async function addToCartFromCategory(event, productId) {
    // 1. TIKLAMA ÇAKIŞMASINI ENGELLE (Çok Önemli)
    if (event) {
        event.preventDefault();
        event.stopPropagation(); // Kartın tıklama olayına gitmesini engeller
    }

    // 2. KULLANICI KONTROLÜ
    const token = localStorage.getItem('jewelry_token');
    if (!token) {
        showNotification("Sepete eklemek için giriş yapmalısınız.", "error");
        return;
    }

    try {
        // Butonu geçici olarak devre dışı bırakalım (çift tıklamayı önlemek için)
        const btn = event?.target.closest('button');
        if (btn) btn.disabled = true;

        const response = await fetch(`http://localhost:5025/api/Cart/items`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                productId: parseInt(productId),
                quantity: 1
            })
        });

        if (response.ok) {
            showNotification("Ürün sepete eklendi!", "success");
            
            // script.js içindeki loadCartFromDb fonksiyonunu tetikleyelim
            if (typeof loadCartFromDb === 'function') {
                await loadCartFromDb(); 
            }

            // Sepet modalını otomatik aç (isteğe bağlı)
            const basketModel = document.querySelector(".basket-model");
            if (basketModel) basketModel.classList.add("active");

        } else {
            const error = await response.json();
            showNotification(error.message || "Ekleme başarısız", "error");
        }
    } catch (error) {
        console.error("Sepet hatası:", error);
        showNotification("Sunucuya bağlanılamadı", "error");
    } finally {
        const btn = event?.target.closest('button');
        if (btn) btn.disabled = false;
    }
}
function updateCartCount() {
    const cart = JSON.parse(localStorage.getItem('cart')) || [];
    // Sepetteki toplam ürün adedini hesapla
    const totalItems = cart.reduce((total, item) => total + item.quantity, 0);
    
    // Navbar'daki sepet sayısı elementini bul (ID'si cartCount olmalı)
    const cartCountElem = document.getElementById('cartCount');
    if (cartCountElem) {
        cartCountElem.textContent = totalItems;
        // Eğer ürün varsa sayıyı göster, yoksa gizle (opsiyonel)
        cartCountElem.style.display = totalItems > 0 ? 'block' : 'none';
    }
}

