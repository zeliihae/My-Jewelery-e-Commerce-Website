// Sepet verilerini saklamak için bir dizi
let cartItems = [];
let allProducts = []; 
async function apiRequest(endpoint, options = {}) {
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        ...options.headers
    };

    const token = localStorage.getItem('jewelry_token') || localStorage.getItem('authToken') || authToken;
    if (token) {
        headers['Authorization'] = `Bearer ${token.replace(/["']/g, "").trim()}`;
    }

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        ...options,
        headers
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`API Error ${response.status}: ${errorText}`);
    }

    // KRİTİK DÜZELTME: Eğer yanıt 204 (No Content) ise json() çağırma
    if (response.status === 204) {
        return null; 
    }

    return await response.json();
}
// DOM elementleri
const searchform = document.querySelector(".search-form");
const categorinav = document.querySelector(".navbar2");
const basketmodel = document.querySelector(".basket-model");
const searchbtn = document.querySelector("#search-btn");
const menubtn = document.querySelector("#menu-btn");
const carticon = document.querySelector(".carticon");
const mark = document.querySelector(".fa-xmark");
const cartList = document.querySelector(".basket-items-list");
const totalElement = document.querySelector(".basket-total .total");
let activeCoupon = null;

// Arama kutusu elementlerini seç
const searchBox = document.querySelector("#search-box");
const searchIcon = document.querySelector(".search-form i");

document.addEventListener("DOMContentLoaded", function() {
    const token = localStorage.getItem('jewelry_token');
    
    if (token) {
        loadCartFromDb(); 
    } else {
        loadCart(); 
    }
});





async function addProductToCart(name, price, img, productId) {
    const currentUser = getCurrentUser();
    if (!currentUser) {
        showNotification("Sepete eklemek için giriş yapmalısınız.", "error");
        setTimeout(() => {
            if (confirm("Giriş yapmak ister misiniz?")) {
                window.location.href = "Login.html";
            }
        }, 500);
        return;
    }

    try {
        const response = await fetch(`/api/Cart/items`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('jewelry_token')}`
            },
            body: JSON.stringify({
                productId: parseInt(productId),
                quantity: 1
            })
        });

        if (response.ok) {
            showNotification("Ürün sepete eklendi!", "success");
            await loadCartFromDb(); 
            
            // Sepet modalini aç
            if (basketmodel) {
                basketmodel.classList.add("active");
            }
        } else {
            const errorData = await response.json().catch(() => ({}));
            showNotification(errorData.message || "Ürün eklenemedi", "error");
        }
    } catch (error) {
        console.error("Sepete ekleme hatası:", error);
        showNotification("Bağlantı hatası oluştu", "error");
    }
}

async function loadNewestProducts() {
    try {
        // YETKİ GEREKTİRMEYEN (Public) adrese istek atıyoruz
        const response = await fetch(`/api/Products?pageSize=10`);

        // KRİTİK DÜZELTME: Eğer sunucudan hata dönerse JSON'a çevirmeye çalışma, direkt hataya düş
        if (!response.ok) {
            throw new Error(`Ürünler yüklenirken sunucu hatası oluştu: ${response.status}`);
        }

        const result = await response.json();

        const rawProducts = result.data?.products || result.products || [];

        allProducts = rawProducts.map(p => ({
            id: p.productId,
            name: p.productName,
            price: p.productPrice,
            discountPrice: p.productDiscountPrice,
            stock: p.productStock,
            image: `images/${p.productImage}`,
            category: p.categoryName
        }));

        const boxContainer = document.querySelector('.newest .box-container');
        if (!boxContainer) return;

        if (allProducts.length === 0) {
            boxContainer.innerHTML = '<div class="empty-msg">Henüz ürün eklenmemiş.</div>';
            return;
        }

        boxContainer.innerHTML = '';
        allProducts.forEach(product => {
            const hasDiscount = product.discountPrice && product.discountPrice < product.price;
            const displayPrice = hasDiscount ? product.discountPrice : product.price;

            const box = document.createElement('div');
            box.className = 'box product-card';
            box.setAttribute('data-product-id', product.id);

            box.innerHTML = `
                <div class="product-image">
                    <button class="favorite-btn" data-product-id="${product.id}">
                        <i class="far fa-heart"></i>
                    </button>
                    <img src="${product.image}" alt="${product.name}" onerror="this.src='images/logo.png'">
                </div>
                <div class="product-details">
                    <h3 class="product-name">${product.name}</h3>
                    <div class="product-price">
                        <span class="current-price">${displayPrice.toFixed(2)} ₺</span>
                        ${hasDiscount ? `<span class="old-price">${product.price.toFixed(2)} ₺</span>` : ''}
                    </div>
                    <button class="btn-add-cart" data-product-id="${product.id}" ${product.stock <= 0 ? 'disabled' : ''}>
                        <i class="fas fa-shopping-cart"></i> ${product.stock > 0 ? 'Add to Cart' : 'Stokta Yok'}
                    </button>
                </div>
            `;
            boxContainer.appendChild(box);

            // Favori durumunu backend'den sorgula
            updateFavoriteButtonState(product.id, box);
        });

        // Event listener'ları ekle
        attachCartButtonListeners();

    } catch (error) {
        // Hata artık sayfayı çökertmeyecek, sadece konsola yazılacak
        console.error("Ana sayfa ürünleri yüklenemedi:", error.message);
    }
}

function attachCartButtonListeners() {
    const cartButtons = document.querySelectorAll('.btn-add-cart:not([disabled])');
    
    cartButtons.forEach(btn => {
        // Önceki listener'ı temizle
        btn.replaceWith(btn.cloneNode(true));
    });
    
    // Yeni listener'ları ekle
    const freshButtons = document.querySelectorAll('.btn-add-cart:not([disabled])');
    freshButtons.forEach(btn => {
        btn.addEventListener('click', async function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const productId = this.getAttribute('data-product-id');
            const product = allProducts.find(p => p.id == productId);
            
            if (!product) {
                console.error("Ürün bulunamadı ID:", productId);
                return;
            }

            if (product.stock <= 0) {
                showNotification('Bu ürün stokta kalmamıştır.', 'error');
                return;
            }

            await addProductToCart(product.name, null, null, productId);
        });
    });
    
    // Favori butonlarına da listener ekle
    attachFavoriteButtonListeners();
}

function attachFavoriteButtonListeners() {
    const favoriteButtons = document.querySelectorAll('.favorite-btn');
    
    favoriteButtons.forEach(btn => {
        // Önceki listener'ı temizle
        btn.replaceWith(btn.cloneNode(true));
    });
    
    // Yeni listener'ları ekle
    const freshFavButtons = document.querySelectorAll('.favorite-btn');
    freshFavButtons.forEach(btn => {
        btn.addEventListener('click', async function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const productId = this.getAttribute('data-product-id');
            await handleFavoriteClick(e, productId);
        });
    });
}


async function handleFavoriteClick(event, productId) {
    event.preventDefault();
    event.stopPropagation();
    
    const btn = event.currentTarget || event.target.closest('.favorite-btn');
    if (!btn) return;

    try {
        const result = await toggleProductFavorite(productId); 
        
        const icon = btn.querySelector('i');
        if (icon) {
            if (result) {
                icon.className = 'fas fa-heart';
                btn.classList.add('active');
            } else {
                icon.className = 'far fa-heart';
                btn.classList.remove('active');
            }
        }
    } catch (error) {
        console.error("Favori işlemi hatası:", error);
    }
}

async function toggleProductFavorite(productId) {
    try {
        const response = await apiRequest('/Favorites/toggle', {
            method: 'POST',
            body: JSON.stringify({ productId: parseInt(productId) })
        });
        
        if (response.isFavorite) {
            showNotification('Ürün favorilere eklendi', 'success');
        } else {
            showNotification('Ürün favorilerden çıkarıldı', 'info');
        }
        
        return response.isFavorite;
    } catch (error) {
        console.error('Toggle favori hatası:', error);
        showNotification('Favori işlemi için giriş yapmalısınız', 'error');
        throw error;
    }
}

async function updateFavoriteButtonState(productId, cardElement) {
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
        console.warn(`${productId} nolu ürünün favori durumu sorgulanamadı.`);
    }
}

function renderSearchResults(products) {
    const boxContainer = document.querySelector('.newest .box-container');
    boxContainer.innerHTML = ''; 

    products.forEach(product => {
        const hasDiscount = product.productDiscountPrice && product.productDiscountPrice < product.productPrice;
        const displayPrice = hasDiscount ? product.productDiscountPrice : product.productPrice;

        const box = document.createElement('div');
        box.className = 'box product-card';
        box.setAttribute('data-product-id', product.productId);

        box.innerHTML = `
            <div class="product-image">
                <button class="favorite-btn" data-product-id="${product.productId}">
                    <i class="far fa-heart"></i>
                </button>
                <img src="images/${product.productImage}" alt="${product.productName}" onerror="this.src='images/logo.png'">
            </div>
            <div class="product-details">
                <h3 class="product-name">${product.productName}</h3>
                <div class="product-price">
                    <span class="current-price">${displayPrice.toFixed(2)} ₺</span>
                    ${hasDiscount ? `<span class="old-price">${product.productPrice.toFixed(2)} ₺</span>` : ''}
                </div>
                <button class="btn-add-cart" data-product-id="${product.productId}" ${product.productStock <= 0 ? 'disabled' : ''}>
                    <i class="fas fa-shopping-cart"></i> ${product.productStock > 0 ? 'Add to Cart' : 'Stokta Yok'}
                </button>
            </div>
        `;
        boxContainer.appendChild(box);
        
        updateFavoriteButtonState(product.productId, box);
    });
    
    // Yeni eklenen butonlara listener ekle
    attachCartButtonListeners();
}


if (searchIcon) {
    searchIcon.addEventListener("click", function () {
        const term = searchBox.value.trim();
        if (term && (document.getElementById('results') || document.querySelector('.newest'))) {
            searchAllProducts(term);
        }
    });
}



if (carticon && basketmodel) {
    carticon.addEventListener("click", function () {
        basketmodel.classList.toggle("active");
    });
} 


if (mark && basketmodel) {
    mark.addEventListener("click", function () {
        basketmodel.classList.remove("active");
    });
}



// Click outside handler'ı sadece bir kez tanımla
function handleClickOutside(e) {
    // Search form kontrolü
    if (searchform && searchform.classList.contains("active")) {
        if (!searchform.contains(e.target) && !searchbtn.contains(e.target)) {
            searchform.classList.remove("active");
        }
    }
    
    // Categories nav kontrolü
    if (categorinav && categorinav.classList.contains("active")) {
        if (!categorinav.contains(e.target) && !menubtn.contains(e.target)) {
            categorinav.classList.remove("active");
        }
    }
}
   // Sadece BİR KEZ document'a ekle
document.addEventListener("click", handleClickOutside);

// Search button
if (searchbtn && searchform) {
    searchbtn.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();
        searchform.classList.toggle("active");
        // Diğer menüleri kapat
        if (categorinav) categorinav.classList.remove("active");
    });
}


if (menubtn && categorinav) {
    menubtn.addEventListener("click", function (e) {
        e.stopPropagation(); // Tıklamanın dökümana yayılmasını engelle
        categorinav.classList.toggle("active");
    });

    // Dökümana tıklama olayını sadece BİR KEZ ve butonun DIŞINDA tanımlıyoruz
    document.addEventListener("click", function (e) {
        // Eğer menü açıksa ve tıklanan yer menü veya buton değilse kapat
        if (categorinav.classList.contains("active")) {
            if (!categorinav.contains(e.target) && !menubtn.contains(e.target)) {
                categorinav.classList.remove("active");
            }
        }
    });
}
async function loadDynamicCategories() {
    try {
        const response = await fetch("/api/Categories");

        // KRİTİK DÜZELTME: Kategoriler yüklenemezse JSON'ı parse etmeden durdur
        if (!response.ok) {
            throw new Error(`Kategoriler yüklenirken sunucu hatası oluştu: ${response.status}`);
        }

        const result = await response.json();

        if (result.success && result.data) {
            const navbar2 = document.querySelector('.navbar2');
            if (!navbar2) return;

            // 1. Önce "Tüm Ürünler" butonunu ekle
            navbar2.innerHTML = `<a href="category.html?cat=Tüm Ürünler">Tüm Ürünler <i class="fa-solid fa-gem"></i></a>`;

            // 2. Diğer kategorileri ekle
            result.data.forEach(cat => {
                const navLink = document.createElement('a');
                navLink.href = `category.html?cat=${encodeURIComponent(cat.categoryName)}`;
                navLink.innerHTML = `${cat.categoryName} <i class="fa-solid fa-angle-down"></i>`;
                navbar2.appendChild(navLink);
            });
        }
    } catch (error) {
        // Uygulamanın çökmesini önle
        console.error("Kategori menüsü yüklenemedi:", error.message);
    }
}


document.addEventListener('DOMContentLoaded', loadDynamicCategories);



document.addEventListener("click", function (e) {
    if (e.target.closest(".btn-add-cart")) {
        e.preventDefault();
        e.stopPropagation();
        
        const productBox = e.target.closest(".product-card");
        if (!productBox) return;

        const name = productBox.querySelector(".product-name").innerText;
        const priceText = productBox.querySelector(".current-price").innerText;
        const price = parseFloat(priceText.replace("₺", "").trim());
        const img = productBox.querySelector("img").src;
        const productId = productBox.getAttribute("data-product-id");

        addProductToCart(name, price, img, Number(productId));

        basketmodel.classList.add("active");
    }
});



async function searchAllProducts(searchTerm) {
    // 1. Elementleri seç
    const boxContainer = document.querySelector('.newest .box-container');
    const titleElement = document.querySelector('.newest h2');
    const newestSection = document.getElementById('results') || document.querySelector('.newest');

    // 2. Güvenlik Kontrolü: Eğer sayfada bu alanlar yoksa (örn: sepet sayfasındaysan) çalışma
    if (!boxContainer || !titleElement) return;

    try {
        // 3. Yükleniyor durumu göster
        boxContainer.innerHTML = `
            <div style="grid-column: 1/-1; text-align:center; padding:50px;">
                <i class="fas fa-spinner fa-spin" style="font-size: 3rem; color: #8B2F39;"></i>
                <p>Ürünler aranıyor...</p>
            </div>`;

        // 4. API İsteği
        const response = await fetch(`/api/Products?search=${encodeURIComponent(searchTerm)}`);
        const result = await response.json();
        
        // API yapına göre datayı al
        const products = result.data || result;

        // 5. Sonuç Kontrolü
        if (!products || products.length === 0) {
            titleElement.innerHTML = `<span>"${searchTerm}"</span> İÇİN SONUÇ BULUNAMADI`;
            boxContainer.innerHTML = '<p class="empty-msg" style="grid-column: 1/-1; text-align:center;">Maalesef kriterlerinize uygun ürün bulamadık.</p>';
        } else {
            titleElement.innerHTML = `ARAMA SONUÇLARI: <span>"${searchTerm}"</span>`;
            renderSearchResults(products); // Ürünleri ekrana basan yardımcı fonksiyonun
        }

        // 6. Sayfayı Arama Sonuçlarına Kaydır
        if (newestSection) {
            const headerHeight = document.querySelector('.header').offsetHeight;
            const targetPosition = newestSection.offsetTop - headerHeight;
            window.scrollTo({ top: targetPosition, behavior: "smooth" });
        }

    } catch (error) {
        console.error("Arama hatası:", error);
        boxContainer.innerHTML = '<p class="error-msg">Arama sırasında bir hata oluştu.</p>';
    }
}




function renderSearchResults(products) {
    const boxContainer = document.querySelector('.newest .box-container');
    boxContainer.innerHTML = ''; 

    products.forEach(product => {
        const hasDiscount = product.productDiscountPrice && product.productDiscountPrice < product.productPrice;
        const displayPrice = hasDiscount ? product.productDiscountPrice : product.productPrice;

        const box = document.createElement('div');
        box.className = 'box product-card';
        box.setAttribute('data-product-id', product.productId);

        box.innerHTML = `
            <div class="product-image">
                <button class="favorite-btn" onclick="handleFavoriteClick(event, ${product.productId})">
                    <i class="far fa-heart"></i>
                </button>
                <img src="images/${product.productImage}" alt="${product.productName}" onerror="this.src='images/logo.png'">
            </div>
            <div class="product-details">
                <h3 class="product-name">${product.productName}</h3>
                <div class="product-price">
                    <span class="current-price">${displayPrice.toFixed(2)} ₺</span>
                    ${hasDiscount ? `<span class="old-price">${product.productPrice.toFixed(2)} ₺</span>` : ''}
                </div>
                <button class="btn-add-cart" ${product.productStock <= 0 ? 'disabled style="opacity:0.6;cursor:not-allowed;"' : ''}>
                    <i class="fas fa-shopping-cart"></i> ${product.productStock > 0 ? 'Add to Cart' : 'Stokta Yok'}
                </button>
            </div>
        `;
        boxContainer.appendChild(box);

        // Sepete ekle butonu
        const cartBtn = box.querySelector(".btn-add-cart");
        if (cartBtn && product.productStock > 0) {
            cartBtn.addEventListener("click", async function (e) {
                e.preventDefault();
                e.stopPropagation();
                await addProductToCart(product.productName, null, null, product.productId);
            });
        }
        
        // Favori durumunu sorgula
        updateFavoriteButtonState(product.productId, box);
    });
}


async function handleFavoriteClick(event, productId) {
    event.preventDefault();
    event.stopPropagation();
    
    const btn = event.currentTarget || event.target.closest('.favorite-btn');
    if (!btn) return;

    try {
        const result = await toggleProductFavorite(productId); 
        
        const icon = btn.querySelector('i');
        if (icon) {
            if (result) {
                icon.className = 'fas fa-heart';
                btn.classList.add('active');
            } else {
                icon.className = 'far fa-heart';
                btn.classList.remove('active');
            }
        }
    } catch (error) {
        console.error("Favori işlemi hatası:", error);
    }
}

async function updateFavoriteButtonState(productId, cardElement) {
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
        console.warn(`${productId} nolu ürünün favori durumu sorgulanamadı.`);
    }
}


function addFavoriteButtons() {
    const productBoxes = document.querySelectorAll(".box");
    
    productBoxes.forEach(box => {
      
        if (box.querySelector('.favorite-btn')) return;
        
        const favoriteBtn = document.createElement('button');
        favoriteBtn.className = 'favorite-btn';
        favoriteBtn.innerHTML = '<i class="far fa-heart"></i>';
        favoriteBtn.title = 'Add to Favorites';
        
        favoriteBtn.addEventListener('click', function(e) {
            e.preventDefault();
            toggleFavorite(box);
        });
        
        box.style.position = 'relative';
        box.insertBefore(favoriteBtn, box.firstChild);
      
        updateFavoriteButton(box);
    });
}

async function toggleProductFavorite(productId) {
    try {
       
        const response = await apiRequest('/Favorites/toggle', {
            method: 'POST',
            body: JSON.stringify({ productId: parseInt(productId) })
        });
        
        if (response.isFavorite) {
            showNotification('Product added to favorites', 'success');
        } else {
            showNotification('Product removed to favorites', 'info');
        }
        
        return response.isFavorite;
    } catch (error) {
        console.error('Toggle favori error:', error);
        throw error;
    }
}


async function toggleFavorite(productBox) {
    const productId = productBox.getAttribute("data-product-id");
    if (!productId) return;

    try {
        const result = await toggleProductFavorite(productId);
        
        const favoriteBtn = productBox.querySelector('.favorite-btn');
        const icon = favoriteBtn.querySelector('i');
        
        // UI Güncelleme
        if (result) {
            icon.className = 'fas fa-heart';
            favoriteBtn.classList.add('active');
        } else {
            icon.className = 'far fa-heart';
            favoriteBtn.classList.remove('active');
        }
    } catch (error) {
        console.error("Favorite operation failed:", error);
    }
}


async function updateFavoriteButton(productBox) {
    const productId = productBox.getAttribute("data-product-id");
    if (!productId) return;

    try {
       
        const isFavorite = await apiRequest(`/Favorites/check/${productId}`);
        
        const icon = productBox.querySelector('.favorite-btn i');
        if (isFavorite) {
            icon.className = 'fas fa-heart';
            productBox.querySelector('.favorite-btn').classList.add('active');
        }
    } catch (error) {
        console.warn("Favorite status could not be checked.");
    }
}


async function loadCartFromDb() {
    try {
        const response = await fetch(`/api/Cart`, {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('jewelry_token')}` }
        });
        const result = await response.json();

        if (response.ok && result.success) {
            // Backend'den gelen her bir ürün için stok bilgisini al
            const itemsWithStock = await Promise.all(
                result.data.items.map(async (item) => {
                    // Ürünün gerçek stok bilgisini backend'den çek
                    let actualStock = 999; // Varsayılan
                    try {
                        const productResponse = await fetch(`/api/admin/Products/${item.productId}`);
                        if (productResponse.ok) {
                            const productData = await productResponse.json();
                            actualStock = productData.productStock || productData.data?.productStock || 999;
                        }
                    } catch (err) {
                        console.warn(`Ürün ${item.productId} stok bilgisi alınamadı`);
                    }
                    
                    return {
                        id: item.cartItemId,
                        productId: item.productId,
                        name: item.productName,
                        price: item.productDiscountPrice ?? item.productPrice,
                        img: `images/${item.productImage}`,
                        quantity: item.quantity,
                        maxStock: actualStock
                    };
                })
            );
            
            cartItems = itemsWithStock;
            updateCartDisplay();
            updateTotal(result.data.totalPrice); 
            updateCartIcon(result.data.totalItems);
        }
    } catch (e) {
        console.error('Sepet yükleme hatası:', e);
    }
}

function updateCartDisplay() {
    if (!cartList) return;
    
    cartList.innerHTML = '';
    
    if (cartItems.length === 0) {
        cartList.innerHTML = `
            <div class="empty-cart-message">
                <i class="fas fa-shopping-cart" style="font-size: 4rem; color: #ddd; margin-bottom: 1rem;"></i>
                <p style="font-size: 1.6rem; color: #999;">Sepetiniz boş</p>
                <p style="font-size: 1.3rem; color: #bbb; margin-top: 0.5rem;">Alışverişe başlamak için ürünleri keşfedin!</p>
            </div>
        `;
        updateTotal();
        return;
    }
    
    cartItems.forEach(item => {
        const itemDiv = document.createElement('div');
        itemDiv.className = 'cart-item';
        itemDiv.setAttribute('data-id', item.id);
        
        // Stok durumu kontrolü
        const stockWarning = item.quantity >= item.maxStock && item.maxStock !== 999 
            ? `<span class="stock-warning"><i class="fas fa-exclamation-triangle"></i> Son ${item.maxStock} adet!</span>` 
            : '';
        
        itemDiv.innerHTML = `
            <div class="item-image-wrapper">
                <img src="${item.img}" alt="${item.name}" onerror="this.src='images/logo.png'">
                <span class="item-quantity-badge">${item.quantity}</span>
            </div>
            <div class="item-details">
                <div class="item-header">
                    <h3 class="item-name">${item.name}</h3>
                    <button class="remove-item" data-id="${item.id}" title="Ürünü Kaldır">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                <p class="item-unit-price">Birim: ${item.price.toFixed(2)} ₺</p>
                ${stockWarning}
                <div class="item-footer">
                    <div class="quantity-controls">
                        <button class="quantity-btn decrease" data-id="${item.id}" title="Azalt" ${item.quantity <= 1 ? 'disabled' : ''}>
                            <i class="fas fa-minus"></i>
                        </button>
                        <span class="quantity-display">${item.quantity}</span>
                        <button class="quantity-btn increase" data-id="${item.id}" title="Artır" ${item.quantity >= item.maxStock && item.maxStock !== 999 ? 'disabled' : ''}>
                            <i class="fas fa-plus"></i>
                        </button>
                    </div>
                    <div class="item-total-price">
                        <span class="total-label">Toplam</span>
                        <span class="total-amount">${(item.price * item.quantity).toFixed(2)} ₺</span>
                    </div>
                </div>
            </div>
        `;
        
        cartList.appendChild(itemDiv);
    });
    
    setupCartButtons();
    updateTotal();
}

// ========================================
// SEPET BUTONLARINI KURMA
// ========================================
function setupCartButtons() {
    // Silme butonları
    document.querySelectorAll(".remove-item").forEach(btn => {
        btn.addEventListener("click", async function() {
            const id = parseInt(this.getAttribute("data-id"));
           await removeFromCart(id);
        });
    });
    
    // Artırma butonları
    document.querySelectorAll(".quantity-btn.increase").forEach(btn => {
        btn.addEventListener("click", async function() {
            const id = parseInt(this.getAttribute("data-id"));
            await updateQuantity(id, 1);
        });
    });
    
    // Azaltma butonları
    document.querySelectorAll(".quantity-btn.decrease").forEach(btn => {
        btn.addEventListener("click", async function() {
            const id = parseInt(this.getAttribute("data-id"));
            const item = cartItems.find(i => i.id === id);
            
            if (item && item.quantity <= 1) {
                if (confirm("Bu ürünü sepetten kaldırmak istiyor musunuz?")) {
                    await removeFromCart(id);
                }
            } else {
                await updateQuantity(id, -1);
            }
        });
    });
}

// ========================================
// SEPETTEN ÜRÜN SİLME 
// ========================================
async function removeFromCart(cartItemId) {
   
    const cartItem = document.querySelector(`.cart-item[data-id="${cartItemId}"]`);
    if (cartItem) {
        cartItem.classList.add('removing');
    }
    
    try {
        const response = await fetch(`/api/Cart/items/${cartItemId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('jewelry_token')}`
            }
        });

        if (response.ok) {
            showNotification("Ürün sepetten kaldırıldı", "info");
            
            // Animasyon bittikten sonra yenile
            setTimeout(async () => {
                await loadCartFromDb();
            }, 300);
        } else {
            showNotification("Ürün kaldırılamadı", "error");
            if (cartItem) {
                cartItem.classList.remove('removing');
            }
        }
    } catch (error) {
        console.error("Silme hatası:", error);
        showNotification("Bağlantı hatası", "error");
        if (cartItem) {
            cartItem.classList.remove('removing');
        }
    }
}

// ========================================
// MİKTAR GÜNCELLEME (STOK KONTROLÜ İLE)
// ========================================
async function updateQuantity(cartItemId, change) {
    const item = cartItems.find(item => item.id === cartItemId);
    if (!item) return;

    const newQuantity = item.quantity + change;

    if (newQuantity <= 0) {
        await removeFromCart(cartItemId);
        return;
    }

    // Stok kontrolü (Frontend)
    if (newQuantity > item.maxStock && item.maxStock !== 999) {
        showNotification(` Maksimum stok: ${item.maxStock} adet. Daha fazla ekleyemezsiniz.`, "error");
        return;
    }

    // Geçici UI güncellemesi (Hızlı geri bildirim)
    const quantityDisplay = document.querySelector(`.cart-item[data-id="${cartItemId}"] .quantity-display`);
    
    if (quantityDisplay) {
        quantityDisplay.style.transform = 'scale(1.2)';
        setTimeout(() => {
            quantityDisplay.style.transform = 'scale(1)';
        }, 200);
    }

    try {
        const response = await fetch(`/api/Cart/items/${cartItemId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('jewelry_token')}`
            },
            body: JSON.stringify({ quantity: newQuantity })
        });

        if (response.ok) {
            await loadCartFromDb();
        } else {
            const errorData = await response.json().catch(() => ({}));
            
            // Backend'den gelen hata mesajını göster
            if (errorData.message) {
                showNotification(errorData.message, "error");
            } else if (change > 0) {
                // Eğer backend mesaj göndermemişse varsayılan mesaj
                showNotification(`⚠️ Bu üründen daha fazla ekleyemezsiniz. Mevcut stok: ${item.maxStock}`, "error");
            } else {
                showNotification("Miktar güncellenemedi", "error");
            }
            
            await loadCartFromDb();
        }
    } catch (error) {
        console.error("Miktar güncelleme hatası:", error);
        showNotification("Bağlantı hatası oluştu", "error");
        await loadCartFromDb();
    }
}


function updateTotal() {
    const totalElement = document.querySelector('.total');
    if (!totalElement) return;

    const subtotal = cartItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    let finalTotal = subtotal;

    if (activeCoupon) {
        finalTotal = subtotal - activeCoupon.discountAmount;
        if (finalTotal < 0) finalTotal = 0;

        totalElement.innerHTML = `
            <div class="total-breakdown">
                <div class="subtotal-row">
                    <span>Ara Toplam:</span>
                    <span>${subtotal.toFixed(2)} ₺</span>
                </div>
                <div class="discount-row">
                    <span><i class="fas fa-tag"></i> İndirim:</span>
                    <span class="discount-amount">-${activeCoupon.discountAmount.toFixed(2)} ₺</span>
                </div>
                <div class="total-row">
                    <span>Ödenecek Tutar:</span>
                    <span class="total-amount">${finalTotal.toFixed(2)} ₺</span>
                </div>
            </div>
        `;
    } else {
        totalElement.innerHTML = `
            <div class="total-breakdown">
                <div class="total-row">
                    <span>Toplam:</span>
                    <span class="total-amount">${finalTotal.toFixed(2)} ₺</span>
                </div>
            </div>
        `;
    }
}


function updateCartIcon(totalItemsFromDb) {
    if (!carticon) return;

    const totalItems = totalItemsFromDb !== undefined 
        ? totalItemsFromDb 
        : cartItems.reduce((sum, item) => sum + item.quantity, 0);
    
    let badge = document.querySelector(".cart-badge");
    
    if (totalItems > 0) {
        if (!badge) {
            badge = document.createElement("span");
            badge.className = "cart-badge";
            carticon.appendChild(badge);
        }
        
        // Değişiklik varsa animasyon
        if (badge.textContent !== totalItems.toString()) {
            badge.style.animation = 'none';
            setTimeout(() => {
                badge.style.animation = 'pulse 0.5s ease';
            }, 10);
        }
        
        badge.textContent = totalItems;
    } else if (badge) {
        badge.style.animation = 'fadeOut 0.3s ease';
        setTimeout(() => badge.remove(), 300);
    }
}

function loadCart() {
    try {
        const savedCart = localStorage.getItem('shoppingCart');
        if (savedCart) {
            cartItems = JSON.parse(savedCart);
            
            if (cartList) {
                updateCartDisplay();
            }
          
            updateCartIcon(); 
        }
    } catch (e) {
        console.error('Yerel sepet yükleme hatası:', e);
    }
}




function saveCart() {
    localStorage.setItem('shoppingCart', JSON.stringify(cartItems));
}

const checkoutBtn = document.querySelector(".basket-total .btn1");
if (checkoutBtn) {
    checkoutBtn.addEventListener("click", function () {
        if (cartItems.length === 0) {
            showNotification("Your cart is empty!", "info");
            return;
        }
        
        const currentUser = getCurrentUser();
        if (!currentUser) {
            if (confirm("You need to log in to place an order.\n\nWould you like to be redirected to the login page?")) {
                saveCart();
                window.location.href = "Login.html";
            }
            return;
        }
        
        // Fonksiyonun tanımlı olduğundan emin olmak için burada çağırıyoruz
        createOrder(); 
    });
}

async function createOrder() {
    console.log(" Sipariş oluşturma başladı!");
    
    const currentUser = getCurrentUser();
    const token = localStorage.getItem('jewelry_token');
    
    if (!currentUser || !token) {
        showNotification("Lütfen önce giriş yapın.", "error");
        return;
    }

    try {
        // 1. ADRES KONTROLÜ
        console.log(" Adres bilgileri çekiliyor...");
        const addrResponse = await fetch(`/api/Addresses/user/${currentUser.id || currentUser.userId}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (!addrResponse.ok) {
            throw new Error("Adres bilgileri alınamadı");
        }
        
        const addresses = await addrResponse.json();
        
        if (!addresses || addresses.length === 0) {
            alert("Lütfen profilinizden bir adres ekleyin.");
            window.location.href = "profile.html";
            return;
        }
        
        const selectedAddress = addresses.find(a => a.isDefault) || addresses[0];
        console.log("Adres seçildi:", selectedAddress.addressId || selectedAddress.id);

        // 2. SİPARİŞ OLUŞTUR (Kupon ile birlikte)
        console.log(" Sipariş oluşturuluyor...");
        
        let orderPayload = {
            shippingAddressId: parseInt(selectedAddress.addressId || selectedAddress.id),
            billingAddressId: parseInt(selectedAddress.addressId || selectedAddress.id),
            paymentMethod: "Credit Card"
        };
        
        //  Eğer aktif kupon varsa ekle
        if (activeCoupon && activeCoupon.couponId) {
            orderPayload.couponId = activeCoupon.couponId;
            console.log(" Kupon ekleniyor:", activeCoupon.couponCode, "ID:", activeCoupon.couponId);
        }
        
        const orderResponse = await fetch(`/api/Orders`, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json', 
                'Authorization': `Bearer ${token}` 
            },
            body: JSON.stringify(orderPayload)
        });

        if (!orderResponse.ok) {
            const errorText = await orderResponse.text();
            console.error(" Sipariş hatası:", errorText);
            throw new Error("Sipariş oluşturulamadı: " + errorText);
        }

        const orderText = await orderResponse.text();
        const orderData = orderText ? JSON.parse(orderText) : null;
        const orderId = orderData?.orderId || orderData?.data?.orderId;

        if (!orderId) {
            console.error(" OrderID alınamadı:", orderData);
            throw new Error("Sipariş ID'si alınamadı");
        }

        console.log(" Sipariş oluşturuldu! Order ID:", orderId);

        // 3. FATURA OLUŞTUR
        await generateReceiptOnBackend(orderId);

    } catch (error) {
        console.error(" Sipariş hatası:", error);
        showNotification("Sipariş tamamlanamadı: " + error.message, "error");
    }
}

async function generateReceiptOnBackend(orderId) {
    try {
        const token = localStorage.getItem('jewelry_token');
        
        console.log(" Fatura oluşturuluyor... Order ID:", orderId);
        
        const response = await fetch(`/api/Receipt`, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ 
                orderId: parseInt(orderId), 
                description: "Online Order"
            })
        });

        const responseText = await response.text();
        console.log(" Backend Response Status:", response.status);
        console.log(" Backend Response:", responseText ? "Veri var" : "Boş");

        if (response.ok) {
            if (responseText && responseText.trim() !== "") {
                try {
                    const receiptData = JSON.parse(responseText);
                    console.log(" Fatura oluşturuldu:", receiptData.receiptNumber);
                    handleOrderSuccess(receiptData);
                    return;
                } catch (parseError) {
                    console.error(" JSON Parse hatası:", parseError);
                }
            }
            
            // Backend 200 döndü ama body boş - GET ile çek
            console.log(" Response boş, GET ile fatura çekiliyor...");
            await fetchExistingReceipt(orderId);
            
        } else if (response.status === 400 && responseText.includes("zaten bir fatura mevcut")) {
            // Fatura zaten var
            console.log("Fatura zaten mevcut, GET ile çekiliyor...");
            await fetchExistingReceipt(orderId);
            
        } else {
            // Diğer hatalar
            console.error(" Backend hatası:", responseText);
            throw new Error("Fatura oluşturulamadı: " + responseText);
        }
        
    } catch (error) {
        console.error(" Fatura oluşturma hatası:", error);
        // Son çare: GET ile dene
        console.log(" Hata nedeniyle GET ile deneniyor...");
        await fetchExistingReceipt(orderId);
    }
}

async function fetchExistingReceipt(orderId, retryCount = 0) {
    const maxRetries = 5;
    const retryDelay = 1000; // 1 saniye
    
    try {
        const token = localStorage.getItem('jewelry_token');
        console.log(` Fatura çekiliyor... Deneme: ${retryCount + 1}/${maxRetries + 1}`);
        
        const response = await fetch(`/api/Receipt/order/${orderId}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.ok) {
            const receiptText = await response.text();
            
            if (receiptText && receiptText.trim() !== "") {
                const receiptData = JSON.parse(receiptText);
                console.log(" Fatura bulundu:", receiptData.receiptNumber);
                handleOrderSuccess(receiptData);
                return;
            } else {
                console.log(" Fatura henüz hazır değil...");
            }
        } else {
            console.error(" GET hatası, Status:", response.status);
        }
        
        // Retry mekanizması
        if (retryCount < maxRetries) {
            console.log(`⏳ ${retryDelay / 1000} saniye sonra tekrar denenecek...`);
            showNotification(`Fatura hazırlanıyor... (${retryCount + 1}/${maxRetries + 1})`, "info");
            
            setTimeout(() => {
                fetchExistingReceipt(orderId, retryCount + 1);
            }, retryDelay);
        } else {
            // Tüm denemeler tükendi
            console.error(" Maksimum deneme sayısına ulaşıldı");
            alert("Siparişiniz başarıyla alındı!\n\nFatura hazırlanamadı. Lütfen 'Siparişlerim' sayfasından kontrol edin.\n\nOrder ID: " + orderId);
            location.reload();
        }
        
    } catch (err) {
        console.error(" Fatura çekme hatası:", err);
        
        if (retryCount < maxRetries) {
            setTimeout(() => {
                fetchExistingReceipt(orderId, retryCount + 1);
            }, retryDelay);
        } else {
            alert("Sipariş alındı ancak fatura yüklenemedi. Lütfen 'Siparişlerim' sayfasından kontrol edin.");
            location.reload();
        }
    }
}

function handleOrderSuccess(receiptData) {
    console.log("🎉 Sipariş başarılı! Sepet temizleniyor...");
    
    // Sepeti temizle
    if (typeof cartItems !== 'undefined') {
        cartItems = [];
    }
    
    if (typeof updateCartDisplay === 'function') {
        updateCartDisplay();
    }
    
    if (typeof updateCartIcon === 'function') {
        updateCartIcon(0);
    }
    
    // ✅ Kuponu temizle
    if (typeof activeCoupon !== 'undefined') {
        activeCoupon = null;
    }
    
    // Kupon UI'ını sıfırla
    const couponInputWrapper = document.querySelector('.coupon-input-wrapper');
    const appliedCoupon = document.getElementById('appliedCoupon');
    const couponInput = document.getElementById('couponInput');
    
    if (couponInputWrapper) couponInputWrapper.style.display = 'flex';
    if (appliedCoupon) appliedCoupon.style.display = 'none';
    if (couponInput) couponInput.value = '';
    
    // Modalı göster
    showReceiptModal(receiptData);
}

function showReceiptModal(receipt) {
    const modal = document.getElementById('receiptModal');
    const content = document.getElementById('receiptContent');
    
    if (!modal || !content) return;

    // Ürün listesini kaydırılabilir bir div içine alıyoruz
    const itemsHtml = receipt.order.orderItems.map(item => `
        <div style="display:flex; justify-content:space-between; margin-bottom:8px; font-size:14px; border-bottom:1px solid #f3f3f3; padding:8px 0; align-items: center;">
            <span style="flex: 1;">${item.quantity}x ${item.product.productName}</span>
            <span style="font-weight:bold; margin-left: 10px;">₺${item.subtotal.toFixed(2)}</span>
        </div>
    `).join('');

    const subtotal = receipt.order.totalAmount;
    const discount = receipt.order.discountAmount || 0;
    const finalTotal = receipt.totalAmount;
    const taxAmount = receipt.taxAmount;
    const baseAmount = finalTotal - taxAmount;

    content.innerHTML = `
        <div style="background:#f8f9fa; padding:12px; border-radius:10px; margin-bottom:15px; border:1px solid #eee; font-size: 13px;">
            <p style="margin:3px 0;"><strong>Fatura No:</strong> ${receipt.receiptNumber}</p>
            <p style="margin:3px 0;"><strong>Tarih:</strong> ${new Date(receipt.receiptDate).toLocaleString('tr-TR')}</p>
            <p style="margin:3px 0;"><strong>Müşteri:</strong> ${receipt.order.user.userName}</p>
        </div>
        
        <div style="margin-bottom:15px;">
            <h4 style="margin-bottom:10px; color:#8B2F39; border-bottom:2px solid #8B2F39; display:inline-block; font-size: 16px;">Sipariş Detayı</h4>
            <div style="max-height: 200px; overflow-y: auto;"> ${itemsHtml}
            </div>
        </div>

        <div style="text-align:right; background:#fff5f5; padding:15px; border-radius:10px; border: 1px solid #ffebeb;">
            <p style="color:#718096; margin:2px 0; font-size:13px;">Ara Toplam: ₺${subtotal.toFixed(2)}</p>
            
            ${discount > 0 ? `
                <p style="color:#27ae60; margin:2px 0; font-size:14px; font-weight:600;">
                    <i class="fas fa-tag"></i> İndirim: -₺${discount.toFixed(2)}
                </p>
            ` : ''}
            
            <p style="color:#888; margin:5px 0; font-size:11px; font-style:italic; border-top: 1px dashed #ddd; padding-top: 5px;">
                (KDV Hariç: ₺${baseAmount.toFixed(2)} + %18 KDV: ₺${taxAmount.toFixed(2)})
            </p>
            
            <h3 style="color:#8B2F39; font-size:22px; margin:8px 0 0 0; font-weight: 800;">
                Toplam: ₺${finalTotal.toFixed(2)}
            </h3>
        </div>
    `;

    if (basketmodel) basketmodel.classList.remove("active");
    modal.style.display = 'flex';
}
function closeReceipt() {
    const modal = document.getElementById('receiptModal');
    if (modal) modal.style.display = 'none';
    window.location.reload(); // Sepeti ve stokları temizlemek için şart
}

function saveOrder(order) {
    let orders = JSON.parse(localStorage.getItem('jewelryOrders') || '[]');
    orders.unshift(order);
    localStorage.setItem('jewelryOrders', JSON.stringify(orders));
    window.dispatchEvent(new CustomEvent('orderCreated', { detail: order }));
}

function updateProductStocks(items) {
    let products = JSON.parse(localStorage.getItem('jewelryProducts') || '[]');
    
    items.forEach(cartItem => {
        const product = products.find(p => p.id == cartItem.productId || p.name === cartItem.name);
        if (product) {
            product.stock = Math.max(0, product.stock - cartItem.quantity);
        }
    });
    
    localStorage.setItem('jewelryProducts', JSON.stringify(products));
    window.dispatchEvent(new CustomEvent('stocksUpdated', { detail: products }));
}
function calculateTotal() {
    const subtotal = cartItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    let discount = 0;
    if (activeCoupon) {
        discount = activeCoupon.type === 'percentage' 
            ? (subtotal * activeCoupon.discount) / 100 
            : activeCoupon.discount;
    }
    return subtotal - discount;
}

function getCurrentUser() {
    try {
        const userData = localStorage.getItem('currentUser');
        if (userData) {
            return JSON.parse(userData);
        }
    } catch (e) {
        console.error('Kullanıcı bilgisi alınırken hata:', e);
    }
    return null;
}

loadCart();
addFavoriteButtons();

function displayUserInfo() {
    try {
        const currentUserData = localStorage.getItem('currentUser');
        if (currentUserData) {
            const user = JSON.parse(currentUserData);
            
            
            const loginLink = document.querySelector('a[href="Login.html"]');
            const signUpLink = document.querySelector('a[href="signup.html"]');
            
            if (loginLink) {
                
                loginLink.href = "profile.html";
                loginLink.innerHTML = `<i class="fas fa-user-circle"></i> ${user.fullName || user.name}`;
                
               
                loginLink.style.fontWeight = "600";
                loginLink.style.color = "var(--burgundy)";
            }

           
            if (signUpLink) {
                signUpLink.style.display = 'none';
            }
        }
    } catch (e) {
        console.error('Kullanıcı bilgisi gösterilirken hata oluştu:', e);
    }
}

function addCouponSection() {
    const basketTotal = document.querySelector('.basket-total');
    if (!basketTotal || document.querySelector('.coupon-section')) return;
    
    const couponSection = document.createElement('div');
    couponSection.className = 'coupon-section';
    couponSection.innerHTML = `
        <div class="coupon-input-wrapper">
            <input type="text" 
                   id="couponInput" 
                   placeholder="Kupon kodunuz varsa giriniz" 
                   class="coupon-input"
                   maxlength="20">
            <button class="coupon-apply-btn" onclick="applyCoupon()">
                <i class="fas fa-tag"></i> Uygula
            </button>
        </div>
        <div id="couponMessage" class="coupon-message"></div>
        <div id="appliedCoupon" class="applied-coupon" style="display: none;">
            <div class="applied-coupon-info">
                <i class="fas fa-check-circle"></i>
                <span id="appliedCouponText"></span>
            </div>
            <button class="remove-coupon-btn" onclick="removeCoupon()">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;
    
    basketTotal.insertBefore(couponSection, basketTotal.firstChild);
}

// script.js içindeki applyCoupon fonksiyonunu bu şekilde güncelle
async function applyCoupon() {
    const couponInput = document.getElementById('couponInput');
    
    // GÜVENLİK KONTROLÜ: Element var mı?
    if (!couponInput) {
        console.error("Hata: couponInput elemanı sayfada bulunamadı!");
        // Eğer eleman yoksa sepet modalını/kupon kısmını tekrar oluşturmayı deneyebiliriz
        addCouponSection(); 
        return;
    }

    const couponCode = couponInput.value.trim().toUpperCase();
    const currentSubtotal = cartItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);

    if (!couponCode) {
        showNotification('Lütfen bir kupon kodu giriniz.', 'info');
        return;
    }

    try {
        const response = await fetch(`/api/Coupons/validate`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                couponCode: couponCode,
                orderAmount: currentSubtotal
            })
        });

        const result = await response.json();

        if (result.isValid) {
            activeCoupon = {
                couponId: result.couponId,
                couponCode: result.couponCode,
                discountAmount: result.discountAmount
            };
            
            // UI Güncelleme (Bu elemanların varlığını da kontrol etmelisin)
            const wrapper = document.querySelector('.coupon-input-wrapper');
            const appliedDiv = document.getElementById('appliedCoupon');
            const appliedText = document.getElementById('appliedCouponText');

            if (wrapper) wrapper.style.display = 'none';
            if (appliedDiv) appliedDiv.style.display = 'flex';
            if (appliedText) appliedText.textContent = `${couponCode} uygulandı!`;
            
            showNotification(result.message, 'success');
            updateTotal();
        } else {
            showNotification(result.message || 'Geçersiz kupon!', 'error');
            activeCoupon = null;
        }
    } catch (error) {
        console.error('Kupon hatası:', error);
        showNotification('Sunucuya bağlanılamadı.', 'error');
    }
}
function removeCoupon() {
    activeCoupon = null;
    document.querySelector('.coupon-input-wrapper').style.display = 'flex';
    document.getElementById('appliedCoupon').style.display = 'none';
    document.getElementById('couponInput').value = '';
    
    // BURASI GÜNCELLENDİ: Sadece toplamları ve ekranı tazele
    updateTotal(); 
    showNotification('Kupon kaldırıldı.', 'info');
}

function showCouponMessage(message, type) {
    const messageDiv = document.getElementById('couponMessage');
    messageDiv.textContent = message;
    messageDiv.className = `coupon-message ${type}`;
    messageDiv.style.display = 'block';
    
    setTimeout(() => {
        messageDiv.style.display = 'none';
    }, 3000);
}

function updateCouponUsage(code) {
    let coupons = JSON.parse(localStorage.getItem('jewelryCoupons') || '[]');
    const coupon = coupons.find(c => c.code === code);
    
    if (coupon) {
        coupon.currentUses = (coupon.currentUses || 0) + 1;
        localStorage.setItem('jewelryCoupons', JSON.stringify(coupons));
        window.dispatchEvent(new CustomEvent('couponUsed', { 
            detail: { code: code, usage: coupon.currentUses } 
        }));
    }
}

window.addEventListener('productsUpdated', function(e) {
    setTimeout(() => {
        addFavoriteButtons();
    }, 100);
});
window.addEventListener('DOMContentLoaded', function() {
    setTimeout(addCouponSection, 100);
});
document.addEventListener('DOMContentLoaded', function() {
    loadNewestProducts();
});

window.addEventListener('productsUpdated', function(e) {
    loadNewestProducts();
});

window.addEventListener('stocksUpdated', function(e) {
    loadNewestProducts();
});

document.addEventListener('click', function(e) {
  
    const productCard = e.target.closest('.box, .product-card');
    
    if (productCard) {
        
        const isButton = e.target.closest('button, .btn, .btn-add-cart, .favorite-btn, a, input');
        
        if (!isButton) {
            const productId = productCard.getAttribute('data-product-id');
            if (productId) {
                e.preventDefault();
                window.location.href = `product-detail.html?id=${productId}`;
            }
        }
    }
});

// ========================================
// BİLDİRİM SİSTEMİ (GÜNCELLENMİŞ)
// ========================================
function showNotification(message, type = 'info') {
    const existingNotification = document.querySelector('.cart-notification');
    if (existingNotification) {
        existingNotification.remove();
    }
    
    const notification = document.createElement('div');
    notification.className = `cart-notification ${type}`;
    
    const iconMap = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-circle',
        'info': 'fa-info-circle'
    };
    
    notification.innerHTML = `
        <i class="fas ${iconMap[type] || 'fa-info-circle'}"></i>
        <span>${message}</span>
    `;
    
    document.body.appendChild(notification);
    
    requestAnimationFrame(() => {
        notification.classList.add('show');
    });
    
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}
window.addEventListener('DOMContentLoaded', function() {
  
    setTimeout(() => {
        if (typeof addCouponSection === 'function') {
            addCouponSection();
        }
    }, 500); 
});