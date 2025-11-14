# Ghid pentru Tipurile de Utilizatori

Aplicația suportă **3 tipuri de utilizatori**:

## 1. **Vizitator Neînregistrat** (Visitor)
- Utilizatori care **nu sunt autentificați**
- Pot accesa doar conținutul public
- Nu au nevoie de un rol în Identity (sunt pur și simplu utilizatori neautentificați)

## 2. **Utilizator Înregistrat** (User)
- Utilizatori care s-au **înregistrat și sunt autentificați**
- Au rolul **"User"** în Identity
- Pot accesa funcționalități suplimentare față de vizitatori

## 3. **Administrator** (Administrator)
- Utilizatori cu **privilegii complete**
- Au rolul **"Administrator"** în Identity
- Pot accesa toate funcționalitățile aplicației

---

## Cum să folosești autorizarea în Controlere

### Exemplu 1: Acțiune accesibilă tuturor (inclusiv vizitatori)
```csharp
public IActionResult PublicContent()
{
    return View();
}
```

### Exemplu 2: Acțiune accesibilă doar utilizatorilor înregistrați
```csharp
[Authorize(Policy = "RequireRegisteredUser")]
public IActionResult RegisteredUserContent()
{
    return View();
}
```

### Exemplu 3: Acțiune accesibilă doar administratorilor
```csharp
[Authorize(Policy = "RequireAdministrator")]
public IActionResult AdminContent()
{
    return View();
}
```

### Exemplu 4: Acțiune accesibilă doar utilizatorilor autentificați (fără verificare de rol)
```csharp
[Authorize]
public IActionResult AuthenticatedContent()
{
    return View();
}
```

---

## Cum să verifici tipul de utilizator în View-uri

### Folosind ViewComponent (Recomandat)
```razor
@await Component.InvokeAsync("UserType")
```

### Verificare directă în View
```razor
@using Microsoft.AspNetCore.Identity
@using MicroSocialPlatform.Models
@using MicroSocialPlatform.Helpers
@inject UserManager<ApplicationUser> UserManager
@inject SignInManager<ApplicationUser> SignInManager

@{
    bool isAuthenticated = SignInManager.IsSignedIn(User);
    ApplicationUser? user = null;
    string userType = "Visitor";
    
    if (isAuthenticated)
    {
        user = await UserManager.GetUserAsync(User);
        userType = await UserHelper.GetUserTypeAsync(UserManager, user, User);
    }
}

@if (userType == "Visitor")
{
    <p>Ești vizitator neînregistrat</p>
}
else if (userType == "User")
{
    <p>Ești utilizator înregistrat: @user?.FullName</p>
}
else if (userType == "Administrator")
{
    <p>Ești administrator: @user?.FullName</p>
}
```

---

## Cum să verifici tipul de utilizator în Controlere

```csharp
using Microsoft.AspNetCore.Identity;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Helpers;

public class MyController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public MyController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> MyAction()
    {
        if (!_signInManager.IsSignedIn(User))
        {
            // Utilizator neautentificat (Vizitator)
            return View("VisitorView");
        }

        var user = await _userManager.GetUserAsync(User);
        var userType = await UserHelper.GetUserTypeAsync(_userManager, user, User);

        if (userType == "Administrator")
        {
            // Logica pentru administrator
        }
        else if (userType == "User")
        {
            // Logica pentru utilizator înregistrat
        }

        return View();
    }
}
```

---

## Helper Methods disponibile

Clasa `UserHelper` oferă următoarele metode statice:

- `IsAdministratorAsync()` - Verifică dacă utilizatorul este administrator
- `IsRegisteredUserAsync()` - Verifică dacă utilizatorul este user înregistrat (nu administrator)
- `IsVisitor()` - Verifică dacă utilizatorul este vizitator neînregistrat
- `GetUserTypeAsync()` - Obține tipul de utilizator ca string ("Visitor", "User", sau "Administrator")

---

## Politici de Autorizare Configurate

În `Program.cs` sunt configurate următoarele politici:

- **"RequireAdministrator"** - Doar administratori
- **"RequireRegisteredUser"** - Utilizatori înregistrați (User sau Administrator)

**Notă**: Vizitatorii nu au nevoie de o politică specială, deoarece implicit toate rutele sunt accesibile dacă nu sunt marcate cu `[Authorize]`.

---

## Exemplu complet: Controller cu toate tipurile

Vezi `ExampleController.cs` pentru exemple complete de utilizare.

