# 🌐 MicroSocialPlatform - Agora

<div align="center">

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9.0+-512BD4?style=for-the-badge&logo=dotnet)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-512BD4?style=for-the-badge&logo=nuget)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=for-the-badge&logo=bootstrap)
![C#](https://img.shields.io/badge/C%23-10.0-239120?style=for-the-badge&logo=c-sharp)

**O platformă socială modernă construită cu ASP.NET Core MVC**

</div>

---

## 📋 Despre Proiect

**MicroSocialPlatform** este o aplicație web de tip rețea socială dezvoltată cu ASP.NET Core MVC, care permite utilizatorilor să se conecteze, să împărtășească conținut multimedia și să interacționeze într-un mediu sigur și personalizabil.

### 🎯 Scopul Proiectului

Această platformă a fost creată pentru a demonstra implementarea unui sistem social complet, cu focus pe:
- **Privacy și Control**: Profiluri publice/private cu sistem avansat de follow
- **Interacțiune Socială**: Like-uri, comentarii, și partajare de conținut multimedia
- **Personalizare**: Profiluri customizabile cu cover photos, status emoji, și biografii
- **Securitate**: Autentificare robustă cu ASP.NET Core Identity

---

## ✨ Features

### 👤 Managementul Utilizatorilor

- **Autentificare Completă**
  - Înregistrare și autentificare securizată
  - Sistem de roluri (Admin, User)
  - Recuperare parolă și confirmare email
  
- **Profiluri Personalizabile**
  - ✅ Poză de profil și cover photo
  - ✅ Nume complet și username personalizat
  - ✅ Bio, locație, website, și dată de naștere
  - ✅ Status personalizat cu emoji
  - ✅ Profiluri publice/private (IsPublic toggle)

### 📝 Postări și Conținut

- **Creare Postări**
  - Text, imagini, și conținut multimedia
  - Suport pentru multiple tipuri de media (PostMedias)
  - Control vizibilitate postări (public/friends/private)
  - Timestamp automat (CreatedAt, UpdatedAt)

- **Interacțiuni**
  - ✅ Like-uri cu tracking utilizator și timestamp
  - ✅ Comentarii cu replies și threading
  - ✅ Counter pentru likes și comentarii
  - ✅ Edit și delete pentru propriile postări

### 👥 Sistem Social

- **Follow System**
  - Urmărește utilizatori cu profiluri publice (instant)
  - Trimite cereri de follow pentru profiluri private
  - Acceptă/Respinge cereri de follow
  - Vizualizează lista de followers și following

- **Feed Personalizat**
  - Feed-ul afișează:
    - Propriile postări
    - Postări de la utilizatori publici
    - Postări de la utilizatori privați urmăriți (cu follow acceptat)
  - Sortare cronologică inversă
  - Paginare pentru performanță optimă

### 🔒 Privacy și Securitate

- **Controlul Vizibilității**
  - Profiluri publice vs. private
  - Vizibilitate configurabilă pentru fiecare postare
  - Doar owner-ul și admin pot edita/șterge conținut
  
- **Sistem de Roluri**
  - **Administrator**: Acces complet la toate postările și utilizatori
  - **User Înregistrat**: Acces personalizat bazat pe relații de follow
  - **User Neînregistrat**: Acces personalizat, poate vedea doar postările publice

### 📱 Interfață Utilizator

- Design responsive cu Bootstrap 5
- Iconițe moderne cu Bootstrap Icons
- Card-uri interactive pentru postări
- Interfață intuitivă și user-friendly

---

## 🛠️ Tehnologii Utilizate

### Backend
- **Framework**: ASP.NET Core MVC
- **ORM**: Entity Framework Core 
- **Bază de Date**: SQL Server / LocalDB
- **Autentificare**: ASP.NET Core Identity

### Frontend
- **UI Framework**: Bootstrap 5.3
- **Iconițe**: Bootstrap Icons
- **Template Engine**: Razor Views
- **JavaScript**: Vanilla JS pentru interacțiuni AJAX

---

## 👨‍💻 Contributors

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/mirunadragunoi">
        <img src="https://github.com/mirunadragunoi.png" width="100px;" alt="Miruna Dragunoi"/><br />
        <sub><b>Miruna Dragunoi</b></sub>
      </a><br />
      💻 🎨 📖
    </td>
    <td align="center">
      <a href="https://github.com/alexandra602">
        <img src="https://github.com/alexandra602.png" width="100px;" alt="Alexandra Panaet"/><br />
        <sub><b>Alexandra Panaet</b></sub>
      </a><br />
      💻 🐛
    </td>
  </tr>
</table>

---

## 📄 Licență

Acest proiect este realizat pentru materia - **Dezvoltarea aplicațiilor WEB - utilizând ASP.NET Core MVC** (profesor Benegui Cezara).

---

## 📧 Contact

**Miruna Dragunoi** - [@mirunadragunoi](https://github.com/mirunadragunoi)

**Alexandra Panaet** - [@alexandra602](https://github.com/alexandra602)

**Project Link**: [https://github.com/mirunadragunoi/MicroSocialPlatform](https://github.com/mirunadragunoi/MicroSocialPlatform)

---

## 🙏 Acknowledgments

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Bootstrap 5](https://getbootstrap.com/)
- [Bootstrap Icons](https://icons.getbootstrap.com/)
- Inspirație de la Instagram, Facebook, și Twitter

---

<div align="center">

**⭐ Dacă îți place acest proiect, lasă un star pe GitHub! ⭐**

Made with ❤️ by the MicroSocialPlatform Team

</div>
