// Kullanıcıları yönetmek için
let users = [];

// Sayfa yüklendiğinde
document.addEventListener('DOMContentLoaded', function() {
    
    loadUsers();
    checkAlreadyLoggedIn();
    
    // Signup formu varsa
    const signupForm = document.getElementById('signupForm');
    if (signupForm) {
        setupSignupForm();
    }
    
    // Login formu varsa
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        setupLoginForm();
    }
});

// Zaten giriş yapılmış mı kontrol et
function checkAlreadyLoggedIn() {
    const currentUser = getCurrentUser();
    if (currentUser) {
        // Kullanıcı zaten giriş yapmış
        if (currentUser.isAdmin) {
            window.location.href = "admin.html";
        } else {
            window.location.href = "index.html";
        }
    }
}

// Signup form setup
function setupSignupForm() {
    const signupForm = document.getElementById('signupForm');
    const passwordInput = document.getElementById('password');
    const confirmPasswordInput = document.getElementById('confirmPassword');
    
    // Şifre gücü kontrolü
    if (passwordInput) {
        passwordInput.addEventListener('input', function() {
            checkPasswordStrength(this.value);
        });
    }
    
    // Form submit
    signupForm.addEventListener('submit', function(e) {
        e.preventDefault();
        handleSignup();
    });
    
    // Şifre eşleşme kontrolü
    if (confirmPasswordInput && passwordInput) {
        confirmPasswordInput.addEventListener('input', function() {
            if (this.value !== passwordInput.value) {
                this.setCustomValidity('Şifreler eşleşmiyor');
            } else {
                this.setCustomValidity('');
            }
        });
    }
}

// Login form setup
function setupLoginForm() {
    const loginForm = document.getElementById('loginForm');
    
    loginForm.addEventListener('submit', function(e) {
        e.preventDefault();
        handleLogin();
    });
}

// Şifre gücü kontrolü
function checkPasswordStrength(password) {
    const strengthBar = document.getElementById('passwordStrength');
    
    if (!strengthBar) return;
    
    let strength = 0;
    
    if (password.length >= 8) strength++;
    if (/[a-z]/.test(password) && /[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;
    
    strengthBar.className = 'password-strength';
    
    if (strength <= 1) {
        strengthBar.classList.add('weak');
    } else if (strength <= 3) {
        strengthBar.classList.add('medium');
    } else {
        strengthBar.classList.add('strong');
    }
}

// Şifre görünürlük toggle
function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    if (!input) return;
    
    const button = input.parentElement.querySelector('.toggle-password');
    if (!button) return;
    
    const icon = button.querySelector('i');
    
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('fa-eye');
        icon.classList.add('fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('fa-eye-slash');
        icon.classList.add('fa-eye');
    }
}

async function handleLogin() {
    const email = document.getElementById('email')?.value.trim() || document.getElementById('loginEmail')?.value.trim();
    const password = document.getElementById('password')?.value || document.getElementById('loginPassword')?.value;

    if (!email || !password) {
        showMessage('Lütfen e-posta ve şifre girin', 'error');
        return;
    }

    try {
        const response = await fetch("http://localhost:5025/api/Auth/Login", {
            method: "POST",
            headers: { 
                "Content-Type": "application/json",
                "Accept": "application/json"
            },
            // Signup'da olduğu gibi beklenen Key isimlerini güncelledik
            body: JSON.stringify({ 
                UserEmail: email,      // 'email' yerine 'UserEmail'
                UserPassword: password  // 'password' yerine 'UserPassword' ve btoa() SİLİNDİ
            })
        });

   if (response.ok) {
            const result = await response.json(); // Backend'den gelen ana obje (success, message, data)
            console.log("Giriş Başarılı! Backend Yanıtı:", result);

            // Veriler 'result.data' içinde olduğu için onu değişkene alıyoruz
            const userData = result.data; 

            if (userData && userData.token) {
                // 1. Token'ı kaydet
                localStorage.setItem('jewelry_token', userData.token); 

                // 2. Admin Kontrolü (Hiyerarşiyi düzelttik: userData.userRole)
                // Backend'den gelen 'userRole' 1 ise admin'dir
                const isAdmin = userData.userRole === 1 || userData.userRole === "1";

                const userPayload = {
                    fullName: userData.userName || 'Kullanıcı',
                
                    userId: userData.userId,
                    isAdmin: isAdmin
                };
                
                // 3. Bilgileri LocalStorage'a kaydet
                localStorage.setItem('currentUser', JSON.stringify(userPayload));

                showMessage('Giriş başarılı! Yönlendiriliyorsunuz...', 'success');
                
                // 4. Doğru Sayfaya Yönlendirme
                setTimeout(() => {
                    if (userPayload.isAdmin) {
                        console.log("Admin tespit edildi, admin.html'e gidiliyor...");
                        window.location.href = 'admin.html';
                    } else {
                        console.log("Müşteri tespit edildi, index.html'e gidiliyor...");
                        window.location.href = 'index.html';
                    }
                }, 1000);
            } else {
                showMessage('Hata: Kullanıcı verileri alınamadı.', 'error');
            }
        
}
    } catch (error) {
        console.error("Login Hatası:", error);
        showMessage('Sunucu bağlantı hatası!', 'error');
    }
}
async function handleSignup() {
    const fullName = document.getElementById('fullName').value.trim();
    const email = document.getElementById('email').value.trim();
    const phone = document.getElementById('phone')?.value.trim() || ""; // Telefon gereklidir hatası için boş olmamalı
    const password = document.getElementById('password').value;

    if (!fullName || !email || !password) {
        showMessage('Lütfen tüm alanları doldurun', 'error');
        return;
    }

    try {
        const response = await fetch("http://localhost:5025/api/Auth/Register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            // Hata aldığın kilit nokta burası: Backend'in beklediği isimler (Key)
            body: JSON.stringify({ 
                UserName: fullName,      // fullName değil, UserName
                UserEmail: email,        // email değil, UserEmail
                UserPhone: phone,        // phone değil, UserPhone
                UserPassword: password   // password değil, UserPassword
            })
        });

        if (response.ok) {
            showMessage('Kayıt başarılı! Giriş sayfasına yönlendiriliyorsunuz...', 'success');
            setTimeout(() => { window.location.href = 'login.html'; }, 1500);
        } else {
            // Backend'den gelen detaylı hata mesajını yakala
            const errorResult = await response.json();
            console.error("Validasyon Hataları:", errorResult.errors);
            showMessage('Kayıt başarısız! Form bilgilerini kontrol edin.', 'error');
        }
    } catch (error) {
        console.error("Bağlantı hatası:", error);
        showMessage('Sunucuya ulaşılamadı!', 'error');
    }
}
// Mevcut kullanıcıyı ayarla
function setCurrentUser(user) {
    try {
        localStorage.setItem('currentUser', JSON.stringify(user));
    } catch (e) {
        console.log('Kullanıcı kaydedildi');
    }
}

// Mevcut kullanıcıyı al
function getCurrentUser() {
    try {
        const userData = localStorage.getItem('currentUser');
        if (userData) {
            return JSON.parse(userData);
        }
    } catch (e) {
        console.log('Kullanıcı bilgisi alındı');
    }
    return null;
}

// Kullanıcıları kaydet
function saveUsers() {
    try {
        localStorage.setItem('jewelryUsers', JSON.stringify(users));
    } catch (e) {
        console.log('Kullanıcılar kaydedildi');
    }
}

// Kullanıcıları yükle
function loadUsers() {
    try {
        const savedUsers = localStorage.getItem('jewelryUsers');
        if (savedUsers) {
            users = JSON.parse(savedUsers);
        } else {
            // Demo kullanıcı
            users = [
                {
                    id: 1,
                    fullName: 'Test Kullanıcı',
                    email: 'test@test.com',
                    phone: '0555 555 55 55',
                    password: btoa('123456'),
                    registerDate: '01.12.2024',
                    status: 'active'
                }
            ];
            saveUsers();
        }
    } catch (e) {
        console.log('Kullanıcılar yüklendi');
    }
}

// Admin paneline yeni kullanıcı bildir
function notifyAdminNewUser(user) {
    try {
        window.dispatchEvent(new CustomEvent('userRegistered', { detail: user }));
    } catch (e) {
        console.log('Admin bildirildi');
    }
}

// Mesaj göster
function showMessage(message, type = 'info') {
    // Eski mesajı kaldır
    const existingMessage = document.querySelector('.success-message');
    if (existingMessage) {
        existingMessage.remove();
    }
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `success-message ${type}`;
    
    const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';
    const bgColor = type === 'success' ? '#27ae60' : '#e74c3c';
    
    messageDiv.innerHTML = `
        <i class="fas ${icon}"></i>
        <span>${message}</span>
    `;
    
    messageDiv.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: white;
        color: #333;
        padding: 15px 25px;
        border-radius: 8px;
        box-shadow: 0 5px 20px rgba(0, 0, 0, 0.2);
        display: flex;
        align-items: center;
        gap: 12px;
        z-index: 10000;
        animation: slideInRight 0.3s ease;
        border-left: 4px solid ${bgColor};
    `;
    
    messageDiv.querySelector('i').style.color = bgColor;
    
    document.body.appendChild(messageDiv);
    
    setTimeout(() => {
        messageDiv.style.animation = 'slideOutRight 0.3s ease';
        setTimeout(() => messageDiv.remove(), 300);
    }, 3000);
}

// CSS animasyonları ekle
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(100px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100px);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);