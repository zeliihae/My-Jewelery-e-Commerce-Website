const API_BASE_URL = 'http://localhost:5025/api';
let currentProduct = null;
let selectedQuantity = 1;
const urlParams = new URLSearchParams(window.location.search);
const productId = urlParams.get('id');

if (!productId) {
    alert('Product ID not found!');
    window.location.href = 'index.html';
}

document.addEventListener('DOMContentLoaded', () => {
    loadProductDetail(productId);
});

async function loadProductDetail(id) {
    try {
        const token = localStorage.getItem('jewelry_token');
        
        // 1. İstekleri Paralel Başlat
        const [productRes, statsRes] = await Promise.all([
            fetch(`${API_BASE_URL}/admin/Products`), // Not: Tekil ürün çekmek daha iyidir
            fetch(`${API_BASE_URL}/Reviews/product/${id}/stats`, {
                headers: { 'Authorization': `Bearer ${token}` }
            })
        ]);

        // 2. Yanıtların Başarılı Olup Olmadığını Kontrol Et (401 veya 404 hatalarını yakalamak için)
        if (!productRes.ok || !statsRes.ok) {
            if (productRes.status === 401 || statsRes.status === 401) {
                console.warn("Kullanıcı yetkili değil, sınırlı veri gösteriliyor.");
            } else {
                throw new Error('Sunucudan hatalı yanıt geldi.');
            }
        }

        // 3. JSON'a Çevirirken Hata Kontrolü
        const productsResult = productRes.ok ? await productRes.json() : { data: [] };
        const stats = statsRes.ok ? await statsRes.json() : { averageRating: 0, totalReviews: 0 };

        // 4. Ürünü Bulma Mantığı
        const allProducts = productsResult.data?.products || productsResult.products || productsResult.data || productsResult;
        const product = Array.isArray(allProducts) ? allProducts.find(p => p.productId == id || p.id == id) : null;

        if (product) {
            currentProduct = {
                ...product,
                productId: product.productId || product.id,
                productName: product.productName || product.name,
                productPrice: product.productPrice || product.price,
                productImage: product.productImage || 'logo.png',
                averageRating: stats.averageRating || 0,
                totalReviews: stats.totalReviews || 0
            };
            
            renderProductDetail(currentProduct);
            if (token) checkFavoriteStatus(id); // Sadece giriş yapmışsa favori kontrolü yap
            loadReviews(id);
        } else {
            throw new Error('Ürün bulunamadı.');
        }
    } catch (error) {
        console.error('Error loading product detail:', error);
        // Kullanıcıya arayüzde hata mesajı gösterilebilir
    }
}

function changeQuantity(delta) {
    const newQuantity = selectedQuantity + delta;
    if (newQuantity >= 1 && newQuantity <= (currentProduct.productStock || 100)) {
        selectedQuantity = newQuantity;
        const display = document.getElementById('quantityDisplay');
        if (display) display.textContent = selectedQuantity;
    } else if (newQuantity > currentProduct.productStock) {
        showNotification('Maximum stock limit reached', 'info');
    }
}

async function addToCart() {
    const token = localStorage.getItem('jewelry_token');
    const pId = productId;

    if (!token) {
        if (confirm('You must log in to add items to the cart.\n\nWould you like to go to the login page?')) {
            window.location.href = 'Login.html';
        }
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/Cart/items`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                productId: parseInt(pId),
                quantity: selectedQuantity
            })
        });

        let result = {};
        const responseText = await response.text();
        if (responseText) {
            result = JSON.parse(responseText);
        }

        if (response.ok) {
            showNotification(`${selectedQuantity} item added to cart! `, 'success');
            if (window.loadCartFromDb) {
                await window.loadCartFromDb(); 
            }
        } else {
            const errorMsg = result.message || "Insufficient stock or error adding product.";
            showNotification(errorMsg, 'error');
        }
    } catch (error) {
        console.error('Add to cart error:', error);
        showNotification('An error occurred during the process.', 'error');
    }
}

function renderProductDetail(product) {
    const hasDiscount = product.productDiscountPrice && product.productDiscountPrice < product.productPrice;
    const displayPrice = hasDiscount ? product.productDiscountPrice : product.productPrice;
    const discountPercent = hasDiscount ? Math.round(((product.productPrice - product.productDiscountPrice) / product.productPrice) * 100) : 0;
    const savings = hasDiscount ? (product.productPrice - product.productDiscountPrice).toFixed(2) : 0;

    // Generate dynamic stars
    const fullStars = Math.floor(product.averageRating);
    const hasHalfStar = product.averageRating % 1 >= 0.5;
    const emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0);

    const starsHtml = `
        ${'<i class="fas fa-star"></i>'.repeat(fullStars)}
        ${hasHalfStar ? '<i class="fas fa-star-half-alt"></i>' : ''}
        ${'<i class="far fa-star"></i>'.repeat(emptyStars)}
    `;

    const html = `
        <div class="product-gallery">
            <div class="main-image-wrapper">
                ${hasDiscount ? `<div class="discount-badge">${discountPercent}% Discount</div>` : ''}
                <button class="favorite-badge" onclick="toggleFavorite()" id="favoriteBtn">
                    <i class="far fa-heart"></i>
                </button>
                <img src="images/${product.productImage}" alt="${product.productName}" class="main-image" ">
            </div>
        </div>
        <div class="product-info">
            <span class="product-category">
                <i class="fas fa-gem"></i> ${product.categoryName}
            </span>
            <h1 class="product-title">${product.productName}</h1>
            
            <div class="product-rating">
                <div class="stars" style="color: #ffc107;">
                    ${starsHtml}
                </div>
                <span class="rating-text">(${product.averageRating.toFixed(1)}/5 - ${product.totalReviews} reviews)</span>
            </div>

            <div class="price-section">
                <div class="price-wrapper">
                    <span class="current-price">${displayPrice.toFixed(2)} ₺</span>
                    ${hasDiscount ? `
                        <span class="old-price">${product.productPrice.toFixed(2)} ₺</span>
                        <span class="savings"><i class="fas fa-tag"></i> ${savings} ₺ savings</span>
                    ` : ''}
                </div>
            </div>
            <div class="stock-status ${product.productStock > 0 ? 'in-stock' : 'out-stock'}">
                <i class="fas ${product.productStock > 0 ? 'fa-check-circle' : 'fa-times-circle'}"></i>
                ${product.productStock > 0 ? `In Stock (${product.productStock} left)` : 'Out of Stock'}
            </div>
            <div class="product-description">
                <strong><i class="fas fa-info-circle"></i> Product Description:</strong><br>
                ${product.productDescription || 'No description available for this item.'}
            </div>
            <div class="quantity-section">
                <div class="section-title">
                    <i class="fas fa-shopping-basket"></i> Quantity
                </div>
                <div class="quantity-controls">
                    <button class="quantity-btn" onclick="changeQuantity(-1)" ${product.productStock === 0 ? 'disabled' : ''}>
                        <i class="fas fa-minus"></i>
                    </button>
                    <span class="quantity-display" id="quantityDisplay">${selectedQuantity}</span>
                    <button class="quantity-btn" onclick="changeQuantity(1)" ${product.productStock === 0 ? 'disabled' : ''}>
                        <i class="fas fa-plus"></i>
                    </button>
                </div>
            </div>
            <div class="action-buttons">
                <button class="btn btn-primary" onclick="addToCart()" ${product.productStock === 0 ? 'disabled' : ''}>
                    <i class="fas fa-shopping-cart"></i>
                    ${product.productStock > 0 ? 'Add to Cart' : 'Out of Stock'}
                </button>
            </div>
            <div class="product-features">
                <h3 style="color: #2d3748; margin-bottom: 15px; font-size: 20px;">
                    <i class="fas fa-star"></i> Product Features
                </h3>
                <div class="features-grid">
                    <div class="feature-item">
                        <div class="feature-icon"><i class="fas fa-truck"></i></div>
                        <span class="feature-text">Free Shipping</span>
                    </div>
                    <div class="feature-item">
                        <div class="feature-icon"><i class="fas fa-undo"></i></div>
                        <span class="feature-text">14-Day Return</span>
                    </div>
                    <div class="feature-item">
                        <div class="feature-icon"><i class="fas fa-certificate"></i></div>
                        <span class="feature-text">Original Product</span>
                    </div>
                    <div class="feature-item">
                        <div class="feature-icon"><i class="fas fa-shield-alt"></i></div>
                        <span class="feature-text">Secure Payment</span>
                    </div>
                </div>
            </div>
        </div>
    `;
    document.getElementById('productDetail').innerHTML = html;
}

async function toggleFavorite() {
    const token = localStorage.getItem('jewelry_token');
    
    if (!token) {
        if (confirm('You must log in for favorite actions.\n\nWould you like to go to the login page?')) {
            window.location.href = 'Login.html';
        }
        return;
    }

    try {
        // Use the global productId variable instead of getAttribute
        const response = await fetch(`${API_BASE_URL}/Favorites/toggle`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ productId: parseInt(productId) })
        });

        if (!response.ok) throw new Error('Toggle failed');

        const result = await response.json();
        const btn = document.getElementById('favoriteBtn');
        
        if (btn) {
            const icon = btn.querySelector('i');
            if (result.isFavorite) {
                icon.className = 'fas fa-heart';
                btn.classList.add('active');
                showNotification('Added to favorites', 'success');
            } else {
                icon.className = 'far fa-heart';
                btn.classList.remove('active');
                showNotification('Removed from favorites', 'info');
            }
        }
    } catch (error) {
        console.error('Favorite action error:', error);
        showNotification('Failed to update favorites', 'error');
    }
}

async function checkFavoriteStatus(id) {
    const token = localStorage.getItem('jewelry_token');
    if (!token) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Favorites/check/${id}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const isFavorite = await response.json();

        const btn = document.getElementById('favoriteBtn');
        if (isFavorite && btn) {
            const icon = btn.querySelector('i');
            icon.className = 'fas fa-heart';
            btn.classList.add('active');
        }
    } catch (error) {
        console.warn('Favorite status check failed');
    }
}

function showNotification(message, type) {
    const colors = {
        success: '#48bb78',
        error: '#f56565',
        info: '#4299e1'
    };

    const notification = document.createElement('div');
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${colors[type] || colors.info};
        color: white;
        padding: 16px 24px;
        border-radius: 10px;
        box-shadow: 0 4px 20px rgba(0,0,0,0.2);
        z-index: 10000;
        animation: slideIn 0.3s ease;
        font-weight: 500;
    `;
    notification.textContent = message;

    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from { transform: translateX(400px); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
    `;
    document.head.appendChild(style);
    document.body.appendChild(notification);

    setTimeout(() => {
        notification.style.animation = 'slideIn 0.3s ease reverse';
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

//Rewievs kıssmı

// Yıldız Seçme Mantığı
let selectedRating = 0;
document.getElementById('starRating').addEventListener('click', (e) => {
    if (e.target.tagName === 'I') {
        selectedRating = parseInt(e.target.getAttribute('data-value'));
        const stars = document.querySelectorAll('#starRating i');
        stars.forEach((s, i) => {
            if (i < selectedRating) {
                s.classList.replace('far', 'fas');
                s.classList.add('active');
            } else {
                s.classList.replace('fas', 'far');
                s.classList.remove('active');
            }
        });
    }
});

// Yorumları Listeleme
async function loadReviews(pId) {
    const token = localStorage.getItem('jewelry_token');
    const reviewsList = document.getElementById('reviewsList');

    if (!token) {
        reviewsList.innerHTML = '<p class="info-msg">Please login to see reviews.</p>';
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/Reviews/product/${pId}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (!response.ok) throw new Error("Unauthorized or not found");

        const reviews = await response.json();

        if (reviews.length === 0) {
            reviewsList.innerHTML = '<p>No reviews yet. Be the first!</p>';
            return;
        }

        reviewsList.innerHTML = reviews.map(r => `
            <div class="review-item" style="border-bottom: 1px solid #eee; padding: 15px 0;">
                <div style="display:flex; justify-content:space-between;">
                    <strong>${r.userName}</strong>
                    <small>${new Date(r.createdAt).toLocaleDateString()}</small>
                </div>
                <div style="color:#ffc107; margin: 5px 0;">
                    ${'<i class="fas fa-star"></i>'.repeat(r.rating)}${'<i class="far fa-star"></i>'.repeat(5 - r.rating)}
                </div>
                <p>${r.comment}</p>
            </div>
        `).join('');
    } catch (error) {
        console.error("Load Reviews Error:", error);
        reviewsList.innerHTML = '<p>Login to see customer feedback.</p>';
    }
}

// Yorum Gönderme
async function submitReview() {
    const token = localStorage.getItem('jewelry_token');
    const userData = JSON.parse(localStorage.getItem('currentUser')); // Kullanıcı verisinden ID al
    const comment = document.getElementById('reviewComment').value.trim();

    if (!token || !userData) {
        showNotification('Please login first', 'error');
        return;
    }

    if (selectedRating === 0 || !comment) {
        showNotification('Please select stars and write a comment', 'info');
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/Reviews`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                productId: parseInt(productId),
                userId: userData.id || userData.userId, // Controller bu ID'yi bekliyor
                rating: selectedRating,
                comment: comment
            })
        });

        const result = await response.json();

        if (response.ok) {
            showNotification('Review submitted successfully!', 'success');
            document.getElementById('reviewComment').value = '';
            loadReviews(productId); // Listeyi tazele
        } else {
            // "Satın almadınız" veya "Zaten yorum yaptınız" hatasını buradan yakalarız
            showNotification(result.message || 'Error submitting review', 'error');
        }
    } catch (error) {
        console.error("Submit error:", error);
        showNotification('Server error or you haven\'t purchased this item.', 'error');
    }
}
