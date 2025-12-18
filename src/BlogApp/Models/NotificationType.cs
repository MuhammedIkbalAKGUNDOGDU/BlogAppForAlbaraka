namespace BlogApp.Models;

public enum NotificationType
{
    UserBanned = 1,          // Kullanıcı yasaklandı
    UserSuspended = 2,       // Kullanıcı askıya alındı
    PostApproved = 3,        // Yazı onaylandı (draft → yayında)
    PostUnpublished = 4,     // Yazı yayından kaldırıldı (yayında → draft)
    PostDeleted = 5          // Yazı kalıcı olarak silindi
}


