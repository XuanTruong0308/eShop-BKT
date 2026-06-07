namespace eShop.Identity.API;

public class UsersSeed(ILogger<UsersSeed> logger, UserManager<ApplicationUser> userManager)
    : IDbSeeder<ApplicationDbContext>
{
    public async Task SeedAsync(ApplicationDbContext context)
    {
        var alice = await userManager.FindByNameAsync("alice");

        if (alice == null)
        {
            alice = new ApplicationUser
            {
                UserName = "alice",
                Email = "AliceSmith@email.com",
                EmailConfirmed = true,
                CardHolderName = "Alice Smith",
                CardNumber = "XXXXXXXXXXXX1881",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/24",
                Id = Guid.NewGuid().ToString(),
                LastName = "Smith",
                Name = "Alice",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "15703 NE 61st Ct",
                SecurityNumber = "123",
            };

            var result = await userManager.CreateAsync(alice, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("alice created");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("alice already exists");
            }
        }

        var bob = await userManager.FindByNameAsync("bob");

        if (bob == null)
        {
            bob = new ApplicationUser
            {
                UserName = "bob",
                Email = "BobSmith@email.com",
                EmailConfirmed = true,
                CardHolderName = "Bob Smith",
                CardNumber = "XXXXXXXXXXXX1881",
                CardType = 1,
                City = "Redmond",
                Country = "U.S.",
                Expiration = "12/24",
                Id = Guid.NewGuid().ToString(),
                LastName = "Smith",
                Name = "Bob",
                PhoneNumber = "1234567890",
                ZipCode = "98052",
                State = "WA",
                Street = "15703 NE 61st Ct",
                SecurityNumber = "456",
            };

            var result = await userManager.CreateAsync(bob, "Pass123$");

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("bob created");
            }
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("bob already exists");
            }
        }

        var admin = await userManager.FindByNameAsync("admin");

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@gmail.com",
                EmailConfirmed = true,
                CardHolderName = "Admin User",
                CardNumber = "XXXXXXXXXXXX9999",
                CardType = 1,
                City = "Da Nang",
                Country = "VN",
                Expiration = "12/30",
                Id = Guid.NewGuid().ToString(),
                LastName = "Admin",
                Name = "Admin",
                PhoneNumber = "0000000000",
                ZipCode = "98052",
                State = "WA",
                Street = "1 Admin St",
                SecurityNumber = "999",
            };

            var result = await userManager.CreateAsync(admin, "Pass0308@");
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            //Gán claim ở config mới tạo role = admin
            await userManager.AddClaimAsync(
                admin,
                new System.Security.Claims.Claim("role", "admin"));

            logger.LogDebug("admin created with role=admin");
        }

        //Cấp role customer cho alice(để test case 2 ra 403)

        if(alice != null)
        {
            var aliceClaims = await userManager.GetClaimsAsync(alice);
            if(!aliceClaims.Any(c => c.Type == "role"))
            {
                await userManager.AddClaimAsync(alice, new System.Security.Claims.Claim("role", "customer"));
            }
        }
    }
}
