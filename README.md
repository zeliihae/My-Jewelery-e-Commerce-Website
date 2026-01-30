# My Jewelery e-Commerce Website
Jewelry Sales E-Commerce Website
Bu proje, Dokuz Eylül Üniversitesi Bilgisayar Mühendisliği Bölümü (CME 3401 DataBase Management System) kapsamında geliştirilmiş,full-stack bir e-ticaret platformudur. 

Proje Genel Bakış
Kullanıcıların hesap oluşturup ürün arayabildiği, alışveriş sepetini yönetebildiği ve güvenli bir şekilde alışveriş yapabildiği modern bir mücevher satış platformudur.Admin paneli ile ürünlerin ve siparişlerin kontrol edilebilmesiyle de tam anlamıyla kullanışlı ve işlevlidir. Projenin odağında, kullanıcı arayüzü ile sorunsuz etkileşime giren, titizlikle tasarlanmış bir veritabanı mimarisi yer almaktadır. 

 Kullanılan Teknolojiler

Backend: C#, ASP.NET Core API 


ORM: Entity Framework Core 


Frontend: HTML, CSS, JavaScript 


Veritabanı: SQL Server Express 


Yetkilendirme: JWT (JSON Web Token) 


Geliştirme Ortamı: Visual Studio 2026 Insider & VS Code 

 Veritabanı Mimarisi (Object-Relational Model)
 
Veritabanı tasarımı 3NF (Üçüncü Normal Form) prensiplerine uygun olarak veri tekrarını önleyecek şekilde tasarlanmıştır. 


Toplam Tablo Sayısı: 15 (Ürünler, Kategoriler, Kullanıcılar, Siparişler, Sepet, Yorumlar, Favoriler, Kuponlar vb.) 


Temel Özellikler

Kullanıcı Yönetimi: JWT tabanlı güvenli oturum açma ve şifrelerin hash'lenerek saklanması. 


Ürün Kataloğu & Arama: Kategorilere göre filtreleme ve gelişmiş kelime bazlı arama sistemi. 


Alışveriş Deneyimi: Alışveriş sepeti yönetimi, kupon kullanımı ve sipariş takibi. 


Admin Paneli: Ürün ekleme/güncelleme, stok yönetimi, kullanıcı durum kontrolü ve indirim/kupon tanımlama yetkileri. 


Etkileşim: Ürünlere yorum yapma, yıldızlı puan verme ve favorilere ekleme. 

 Yazılım Mimarisi
  
Proje Model-View-Controller (MVC) tasarım desenini takip eder: 


Model: EF Core modelleri ve Veri Transfer Objeleri (DTOs). 


View: HTML/JS ile oluşturulmuş responsive arayüzler. 


Controller: Veritabanı operasyonlarını yürüten API sınıfları. 


Geliştirici: Zeliha Erdoğan    
