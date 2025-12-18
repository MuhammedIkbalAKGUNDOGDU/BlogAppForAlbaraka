using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BlogApp.Services;

public class RabbitMQService : IDisposable
{
    private readonly IConnection _connection;  //rabbitmq bağlantı
    private readonly IModel _channel;         
    private readonly string _queueName;       

    public RabbitMQService()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");
        var username = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";
        var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
        _queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE_NAME") ?? "email_queue";

        // Connection factory diye bir şey oluşturmak gerekiyor
        var factory = new ConnectionFactory()
        {
            HostName = host,      // RabbitMQ sunucu adresi
            Port = port,          // RabbitMQ port
            UserName = username,  // Kullanıcı adı
            Password = password   // Şifre
        };

        try
        {
            // Bağlantı oluştur
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();  // Kanal oluştur

            // Queue'yu declare et (yoksa oluşturur, varsa kullanır)
            _channel.QueueDeclare(
                queue: _queueName,           // Queue adı
                durable: true,                // Queue kalıcı olsun (server restart'ta kaybolmasın)
                exclusive: false,             // Sadece bu bağlantıya özel değil
                autoDelete: false,            // Otomatik silinmesin
                arguments: null               // Ek argümanlar yok
            );

            Console.WriteLine($"RabbitMQ bağlantısı başarılı: {host}:{port}, Queue: {_queueName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RabbitMQ bağlantı hatası: {ex.Message}");
            Console.WriteLine($"RabbitMQ Host: {host}, Port: {port}");
            throw;  // Hata durumunda exception fırlat
        }
    }

    // Mesaj gönderme metodu (Producer)
    public void PublishEmailMessage(int postId, int userId)
    {
        try
        {
            // Mesaj objesi oluştur
            var message = new
            {
                PostId = postId,   
                UserId = userId   
            };

            var json = JsonSerializer.Serialize(message);             // JSON'a çevir
            var body = Encoding.UTF8.GetBytes(json);  // Byte array'e çevir

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;  // Mesaj kalıcı olsun (disk'e yazılsın)

            _channel.BasicPublish(
                exchange: "",           // Exchange yok (direct queue)
                routingKey: _queueName, // Queue adı
                basicProperties: properties,  // Mesaj özellikleri
                body: body              // Mesaj içeriği
            );

            Console.WriteLine($"RabbitMQ mesaj gönderildi: PostId={postId}, UserId={userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RabbitMQ mesaj gönderme hatası: {ex.Message}");
            throw;
        }
    }

    // Mesaj okuma için callback ayarla (Consumer için)
    public void StartConsuming(Func<int, int, Task<bool>> messageHandler)
    {
        // Consumer oluştur
        var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(_channel);

        // Mesaj geldiğinde çalışacak event handler
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();  // Mesaj body'sini al
                var message = Encoding.UTF8.GetString(body);  // String'e çevir
                Console.WriteLine($"RabbitMQ mesaj alındı: {message}");
                
                var emailData = JsonSerializer.Deserialize<EmailMessage>(message);  // JSON'dan parse et

                if (emailData != null)
                {
                    Console.WriteLine($"Email gönderme başlatılıyor: PostId={emailData.PostId}, UserId={emailData.UserId}");
                    
                    // Mesajı işle (email gönder)
                    var success = await messageHandler(emailData.PostId, emailData.UserId);

                    if (success)
                    {
                        // Başarılı olursa mesajı queue'dan sil (acknowledge)
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        Console.WriteLine($"Email başarıyla gönderildi ve mesaj queue'dan silindi");
                    }
                    else
                    {
                        // Başarısız olursa mesajı reddet ve tekrar kuyruğa ekle (requeue)
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                        Console.WriteLine($"Email gönderilemedi, mesaj tekrar queue'ya eklendi");
                    }
                }
                else
                {
                    Console.WriteLine("Mesaj parse edilemedi, mesaj reddediliyor");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Consumer hatası: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                // Hata durumunda mesajı requeue etme (sonsuz döngüye girmesin)
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        // Consumer'ı başlat
        _channel.BasicConsume(
            queue: _queueName,      // Dinlenecek queue
            autoAck: false,         // Manuel acknowledge (başarılı olunca sil)
            consumer: consumer       // Consumer instance
        );

        Console.WriteLine($"RabbitMQ consumer başlatıldı, queue dinleniyor: {_queueName}");
    }

    // Dispose pattern - kaynakları temizle
    public void Dispose()
    {
        _channel?.Close();      // Kanalı kapat
        _channel?.Dispose();     // Kanalı dispose et
        _connection?.Close();    // Bağlantıyı kapat
        _connection?.Dispose(); // Bağlantıyı dispose et
    }

    // Mesaj deserialization için class
    private class EmailMessage
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
    }
}
