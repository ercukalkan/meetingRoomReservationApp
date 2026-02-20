Toplantı odası rezervasyon uygulaması:

## Uygulama Yapısı

- API projesi (.NET WebAPI)
- Core projesi (.NET Class Library)
- Data projesi (.NET Class Library)

## Özellikler

- Toplantı odası yönetimi (ekleme, düzenleme, silme)
- Oda ekipmanları yönetimi (ekleme, düzenleme, silme)
- Kullanıcı yönetimi (ekleme, düzenleme, silme)
- Tekrarlı veya tek seferlik rezervasyon yönetimi (ekleme, düzenleme, silme)
- Rezervasyon çakışma kontrolü ve doğrulama

## Kullanılan Teknolojiler

- .NET 9
- Entity Framework Core
- Microsoft SQL Server
- Docker Compose

## Çalıştırma adımları

1. **Reponun local ortama kopyalanması:**
   - git clone https://github.com/kullanici/meetingRoomReservationApp.git
   - cd meetingRoomReservationApp

2. **.NET Bağımlılıkların yüklenmesi:**
   - dotnet restore

3. **Veritabanı bağlantı kontrolün:**
   - Kendi ortamınızda .NET Core User Secrets oluşturup ConnectionStrings göre güncelleyin.
     - cd .\API\
     - dotnet user-secrets init
     - dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server={server};Database={database};User Id={user-id};Password={password};TrustServerCertificate=True"

4. **Environment değişkenleri dosyasının oluşturulması**
   - Docker compose dosyasının okuyabilmesi için proje dizininde .env dosyası oluşturun.
   - MSSQL_SA_PASSWORD ve ACCEPT_EULA değişkenlerini oluşturup kendi değerlerinizi atayın.

5. **Docker ile MSSQL motorunun çalıştırılması:**
   - docker-compose up --build

6. **Veritabanı migration uygulaması ve veritabanı güncellemesi**
   - dotnet ef database update

7. **Uygulamanın başlatılması:**
   - Proje dizininde:
     - dotnet run --project .\API\

8. **Uygulamaya erişim:**
   - Tarayıcıda `http://localhost:5021` adresini ziyaret edin (port yapılandırmasına göre değişebilir).

## Tasarlanan İş Kuralları

- İş kuralları Reservation tipi için konulduğundan ötürü, Reservation tipi için CRUD işlemleri, ReservationHelper sınıfı altında oluşturulan statik metotlarla validasyon kontrolüne sokuldu. İzin verilen değerler (örneğin bir kullanıcı için günlük maksimum toplantı sayısı) hard-coded olarak metotların içine eklendi. Validasyon kontrolünün aşılmadığı durumlarda global exception handler middleware'i ile kullanıcıya açıklayıcı hata mesajları iletildi.

1. **Çakışan rezervasyonlar**
   - Aynı oda için aynı saatte birden fazla rezervasyon yapılamaz.
     - Yeni rezervasyon oluşturulurken mevcut rezervasyonlarla saat çakışmaları kontrol edildi.
     - Çakışma durumunda **_"The reservation overlaps with an existing reservation for the same room."_** hatası fırlatılıyor.
   - Authorization mekanizması eklenerek yetki kapasitesine göre mevcut rezervasyon silinip yenisinin oluşturulması mümkün kılınabilir.

2. **Rezervasyon süreleri**
   - Sadece maksimum rezervasyon süresi 2 saat olacak şekilde eklendi.
     - Maksimum süreyi aşan rezervasyonlar eklenmeye veya güncellenmeye çalışılırken **_"The reservation exceeds the maximum allowed duration of 2 hours."_** hatası fırlatılıyor.
   - Geçmiş tarihli rezervasyon yapılamıyor. **_"Cannot create a reservation from the past."_** hatası fırlatılıyor.
   - 1 haftadan sonraki günler için rezervasyon yapılamıyor. **_"Cannot create a reservation that starts in more than a week from now."_** hatası fırlatılıyor.

3. **İptal politikası**
   - Başlangıç saatine 30 dakikadan az kalan ve başlamış durumda olan rezervasyonlar iptal edilemiyor. **_"Cannot cancel a reservation less than 30 minutes before it starts."_** hatası fırlatılıyor.

4. **Kapasite kontrolü**
   - Kapasite kontrolü eklenmedi.

5. **Kullanıcı kısıtlamaları**
   - Bir kullanıcı için aynı günde en fazla 3 rezervasyon sınırı eklendi. Aşıldığı durumda **_"User cannot have more than 3 active reservations on the same day."_** hatası fırlatılıyor.
   - Aynı kullanıcı için çakışan saatlerde farklı odalara rezervasyon yapılması engellendi. **_"User already has a reservation that overlaps with the new reservation."_** hatası fırlatılıyor.

- Bütün bu iş kuralları Specification Pattern kullanılarak hard-coded olmaktan çıkarılabilir. Ancak bu aşamada Specification Pattern uygulanmadı.

## Tekrarlayan Toplantılar Yaklaşımı

- Tekrarlayan toplantılar (RecurringReservation), tek seferlik toplantılardan (Reservation) farklı bir entity olarak veritabanında tutuluyor.
- Varsayım: **_Tekrarlayan toplantılar her hafta aynı gün olacak şekilde tasarlandı._**
- Bunun için, oluşturulan tekrarlayan rezervasyon tipine (RecurringReservation) toplantıların kaç hafta boyunca tekrar edeceğini belirten bir integer hafta sayısı (NumberOfWeeks) özelliği eklendi.
- Yeni bir **_RecurringReservation_** nesnesi veritabanına eklenirken (RecurringReservations tablosu), kullanıcıdan alınan hafta sayısı özelliğine göre **_Reservation_** nesneleri hafta adedi kadar ve her biri birer hafta atlayacak şekilde oluşturulup ayrıca veritabanına (Reservations tablosu) eklendi.
- Tekrarlayan toplantılar için bir günün resmi tatile denk gelmesi özelliği eklenmedi.

## Veritabanı Şeması

- Tablolar
  - Room, User, Equipment, Reservation, RecurringReservation
- İlişkiler
  - Room-Equipment -> many-to-many, ilişkisel tablo: dbo.RoomEquipment
  - Room-Reservation
    - one-to-many
    - Bir rezervasyonun tek bir odası olabilir. Bir odanın birden fazla rezervasyonu olabilir.
  - Room-RecurringReservation
    - one-to-many
    - Bir tekrarlayan rezervasyonun tek bir odası olabilir. Bir odanın birden fazla tekrarlayan rezervasyonu olabilir.
  - Reservation-RecurringReservation
    - zero-to-one-to-many (optional)
    - Bir rezervasyon bir tekrarlayan rezervasyon tipi oluşturulurken oluşturulmuş olabilir. Ancak bu şart değil.
  - User-Reservation
    - one-to-many
    - Bir rezervasyon tek bir kullanıcı tarafından oluşturulabilir. Bir kullanıcı birden fazla rezervasyon oluşturabilir.
  - User-RecurringReservation
    - one-to-many
    - Bir tekrarlayan rezervasyon tek bir kullanıcı tarafından oluşturulabilir. Bir kullanıcı birden fazla tekrarlayan rezervasyon oluşturabilir.

## API Endpointleri

- Swagger collection ile proje dizininde mevcut.

## Varsayımlar

- Tekrarlayan toplantıların haftalık olacağı varsayıldı.

## Veritabanı migration

- Migration dosyası proje dizininde bulunuyor. (.\Data\Migrations)

## Seed data

- Seed data, Data projesinde DbSeeder klasörünün altında SeedData klasöründe JSON dosyası halinde bulunuyor.
- Veritabanı seeding işlemi, uygulama çalıştırıldığında bu JSON dosyalarının okunup veritabanına eklenmesiyle gerçekleştiriliyor.

## Exception Handling

- Global exception handler middleware sınıfı **_(ExceptionHandlerMiddleware)_** ve WebAPI projesine uygulanması için çalışacak sınıf **_ExceptionHandlerMiddlewareExtensions_** yazılarak, validasyon hatalarının yakalanıp kullanıcıya açıklayıcı hata mesajı olarak gösterilmesi sağlandı.
- Controller sınıflarının tamamına uygulanmadı.

## ResponseSchema

- Request ve Response yapılarında çoğunlukla DTO kullanıldı. Kullanıcıya hassas bilgi taşınmasının önüne geçildi.
- Ayrıca response yapısının standart hale getirilmesi için **_ResponseSchema_** sınıfı oluşturuldu.
- Bu şekilde response'lar kullanıcıya yönergede istenen yapıda gönderildi.
- Sonucu hata vermeyen responselar için de **_ResponseSchema_** sınıfından miras alan generic tipte bir **_ResponseSchema<T>_** sınıfı oluşturuldu. Bu sınıfta, base sınıfta olmayan bir Data özelliği bulunuyor ve veritabanından elde edilen sorgu sonuçları bu özelliğin içinde kullanıcıya gösteriliyor.
