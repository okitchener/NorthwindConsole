using NLog;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using NorthwindConsole.Model;
using System.ComponentModel.DataAnnotations;
string path = Directory.GetCurrentDirectory() + "//nlog.config";

// create instance of Logger
var logger = LogManager.Setup().LoadConfigurationFromFile(path).GetCurrentClassLogger();
Console.Clear();
logger.Info("Program started");

do
{
    Console.WriteLine("1) Categories");
    Console.WriteLine("2) Product");
    Console.WriteLine("3) Delete");
    Console.WriteLine("Enter to quit");
    string? mainChoice = Console.ReadLine();
    Console.Clear();
    logger.Info("Main Option {mainChoice} selected", mainChoice);
    
    if (mainChoice == "1")
    {
        // Categories submenu
        Console.WriteLine("Categories Menu:");
        Console.WriteLine("1) Display categories");
        Console.WriteLine("2) Add category");
        Console.WriteLine("3) Display Category and related products");
        Console.WriteLine("4) Display all Categories and their related products");
        Console.WriteLine("Enter to return to main menu");
        string? choice = Console.ReadLine();
        Console.Clear();
        logger.Info("Category option {choice} selected", choice);    
        if (choice == "1")
        {
            var configuration = new ConfigurationBuilder()
                 .AddJsonFile($"appsettings.json");

            var config = configuration.Build();

            var db = new DataContext();
            var query = db.Categories.OrderBy(p => p.CategoryName);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{query.Count()} records returned");
            Console.ForegroundColor = ConsoleColor.Magenta;
            foreach (var item in query)
            {
                Console.WriteLine($"{item.CategoryName} - {item.Description}");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
        else if (choice == "2")
        {
            Category category = new();
            Console.WriteLine("Enter Category Name:");
            category.CategoryName = Console.ReadLine()!;
            Console.WriteLine("Enter the Category Description:");
            category.Description = Console.ReadLine();
            ValidationContext context = new ValidationContext(category, null, null);
            List<ValidationResult> results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(category, context, results, true);
            if (isValid)
            {
                var db = new DataContext();
                // check for unique name
                if (db.Categories.Any(c => c.CategoryName == category.CategoryName))
                {
                    // generate validation error
                    isValid = false;
                    results.Add(new ValidationResult("Name exists", ["CategoryName"]));
                }
                else
                {
                    logger.Info("Validation passed");
                    // TODO: save category to db
                }
            }
            if (!isValid)
            {
                foreach (var result in results)
                {
                    logger.Error($"{result.MemberNames.First()} : {result.ErrorMessage}");
                }
            }
        }
        else if (choice == "3")
        {
            var db = new DataContext();
            var query = db.Categories.OrderBy(p => p.CategoryId);

            Console.WriteLine("Select the category whose products you want to display:");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            foreach (var item in query)
            {
                Console.WriteLine($"{item.CategoryId}) {item.CategoryName}");
            }
            Console.ForegroundColor = ConsoleColor.White;
            int id = int.Parse(Console.ReadLine()!);
            Console.Clear();
            logger.Info($"CategoryId {id} selected");
            Category category = db.Categories.Include("Products").FirstOrDefault(c => c.CategoryId == id)!;
            Console.WriteLine($"{category.CategoryName} - {category.Description}");
            foreach (Product p in category.Products)
            {
                Console.WriteLine($"\t{p.ProductName}");
            }
        }
        else if (choice == "4")
        {
            var db = new DataContext();
            var query = db.Categories.Include("Products").OrderBy(p => p.CategoryId);
            foreach (var item in query)
            {
                Console.WriteLine($"{item.CategoryName}");
                foreach (Product p in item.Products)
                {
                    Console.WriteLine($"\t{p.ProductName}");
                }
            }
        }
    }
    else if (mainChoice == "2")
    {
        // Products submenu
        Console.WriteLine("Products Menu:");
        Console.WriteLine("1) Add Product");
        Console.WriteLine("2) Edit Product Record");
        Console.WriteLine("3) Display Products");
        Console.WriteLine("4) Display a Specific Product");
        Console.WriteLine("Enter to return to main menu");
        string? choice = Console.ReadLine();
        Console.Clear();
        logger.Info("Product option {choice} selected", choice);

        if (choice == "1")
        {
            var db = new DataContext();
            Product product = new();
            Console.WriteLine("Enter Product Name:");
            product.ProductName = Console.ReadLine()!;
        
        // Display categories for selection
        var categories = db.Categories.OrderBy(c => c.CategoryId);
        Console.WriteLine("Select a category for the product:");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        foreach (var cat in categories)
        {
            Console.WriteLine($"{cat.CategoryId}) {cat.CategoryName}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        int categoryId = int.Parse(Console.ReadLine()!);
        product.CategoryId = categoryId;
        logger.Info($"CategoryId {categoryId} selected for product");
        
        Console.WriteLine("Enter Unit Price:");
        product.UnitPrice = decimal.Parse(Console.ReadLine()!);
        Console.WriteLine("Enter Units In Stock:");
        product.UnitsInStock = short.Parse(Console.ReadLine()!);
        
        ValidationContext context = new ValidationContext(product, null, null);
        List<ValidationResult> results = new List<ValidationResult>();

        // Check for empty product name
        if (string.IsNullOrWhiteSpace(product.ProductName))
        {
            results.Add(new ValidationResult("Product name cannot be empty", ["ProductName"]));
        }

        var isValid = Validator.TryValidateObject(product, context, results, true);
        
        // Override isValid if we have any validation results (including our custom ones)
        isValid = results.Count == 0;
        
        if (isValid)
        {
            // check for unique name
            if (db.Products.Any(p => p.ProductName == product.ProductName))
            {
                // generate validation error
                isValid = false;
                results.Add(new ValidationResult("Product name exists", ["ProductName"]));
            }
            else
            {
                logger.Info("Validation passed");
                // Save product to database
                db.Products.Add(product);
                db.SaveChanges();
                logger.Info($"Product saved: {product.ProductId} - {product.ProductName}");
                Console.WriteLine($"Product '{product.ProductName}' saved successfully with ID: {product.ProductId}");
            }
        }
        if (!isValid)
        {
            foreach (var result in results)
            {
                logger.Error($"{result.MemberNames.First()} : {result.ErrorMessage}");
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
        }
    }
    else if (choice == "2")
    {
        // Edit Product Record
        var db = new DataContext();

        // Displaying products for selection
        var products = db.Products.OrderBy(p => p.ProductId);
        Console.WriteLine("Select a product to edit:");
        Console.ForegroundColor = ConsoleColor.Cyan;
        foreach (var p in products)
        {
            Console.WriteLine($"{p.ProductId}) {p.ProductName} - Price: {p.UnitPrice:C}, Stock: {p.UnitsInStock}, Discontinued: {p.Discontinued}");
        }
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.Write("\nEnter Product ID: ");
        int productId = int.Parse(Console.ReadLine()!);

         // Find the product
        Product? product = db.Products.FirstOrDefault(p => p.ProductId == productId);
         if (product == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Product with ID {productId} not found.");
            Console.ForegroundColor = ConsoleColor.White;
            logger.Error($"Product with ID {productId} not found");
        }
        else
        {
            Console.Clear();
            logger.Info($"Product {productId} - {product.ProductName} selected for editing");


        Console.WriteLine($"Editing Product: {product.ProductName}");
        Console.WriteLine("Choose Product Record to edit");
        Console.WriteLine("1) Product name");
        Console.WriteLine("2) Unit Price");
        Console.WriteLine("3) Units In Stock");
        Console.WriteLine("4) Discontinued status");
        Console.WriteLine("Enter to return to Products menu");
        string? editChoice = Console.ReadLine();
        Console.Clear();
        if (editChoice == "1")
        {
            //Edit Product Name
            Console.WriteLine($"Current Product Name: {product.ProductName}");
            Console.Write("Enter new Product Name: ");
            string? newName = Console.ReadLine();
              if (!string.IsNullOrWhiteSpace(newName))
                {
                    // Check for unique name
                    if (db.Products.Any(p => p.ProductName == newName && p.ProductId != productId))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: Product name already exists.");
                        Console.ForegroundColor = ConsoleColor.White;
                        logger.Error($"Product name '{newName}' already exists");
                    }
                    else
                    {
                        product.ProductName = newName;
                        db.SaveChanges();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Product name updated to '{newName}' successfully!");
                        Console.ForegroundColor = ConsoleColor.White;
                        logger.Info($"Product {productId} name updated to '{newName}'");
                    }
                }

        }
        else if (editChoice == "2")
        {   
            //Edit Unit Price
            Console.WriteLine($"Current Unit Price: {product.UnitPrice:C}");
            Console.Write("Enter new Unit Price: ");
            string? newPriceInput = Console.ReadLine();
               
                if (!string.IsNullOrWhiteSpace(newPriceInput) && decimal.TryParse(newPriceInput, out decimal newPrice))
                {
                    product.UnitPrice = newPrice;
                    db.SaveChanges();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Unit Price updated to {newPrice:C} successfully!");
                    Console.ForegroundColor = ConsoleColor.White;
                    logger.Info($"Product {productId} price updated to {newPrice:C}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid price format.");
                    Console.ForegroundColor = ConsoleColor.White;
                }

        }
        else if (editChoice == "3")
        {
            //Edit Units In Stock
            Console.WriteLine($"Current Units In Stock: {product.UnitsInStock}");
            Console.Write("Enter new Units In Stock: ");
             string? newStockInput = Console.ReadLine();
               
                if (!string.IsNullOrWhiteSpace(newStockInput) && short.TryParse(newStockInput, out short newStock))
                {
                    product.UnitsInStock = newStock;
                    db.SaveChanges();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Units In Stock updated to {newStock} successfully!");
                    Console.ForegroundColor = ConsoleColor.White;
                    logger.Info($"Product {productId} stock updated to {newStock}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid stock quantity format.");
                    Console.ForegroundColor = ConsoleColor.White;
                }

        }
        else if (editChoice == "4")
        {
            //Edit Discontinued Status
            Console.WriteLine($"Current Discontinued Status: {product.Discontinued}");
         Console.Write("Set as discontinued? (y/n): ");
                string? discontinuedInput = Console.ReadLine();
               
                if (!string.IsNullOrWhiteSpace(discontinuedInput))
                {
                    bool newDiscontinued = discontinuedInput.ToLower() == "y" || discontinuedInput.ToLower() == "yes";
                    product.Discontinued = newDiscontinued;
                    db.SaveChanges();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Discontinued status updated to {newDiscontinued} successfully!");
                    Console.ForegroundColor = ConsoleColor.White;
                    logger.Info($"Product {productId} discontinued status updated to {newDiscontinued}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Invalid input for discontinued status.");
                    Console.ForegroundColor = ConsoleColor.White;
        }
    }
        }
    }
    else if (choice == "3")
    {
        //Future Display products
        Console.WriteLine("What products do you wish the display?");
        Console.WriteLine("1) Display all products");
        Console.WriteLine("2) Display all active products");
        Console.WriteLine("3) Display all discontinued products");
        Console.WriteLine("Enter to return to Products menu");
        string? displayChoice = Console.ReadLine();
        Console.Clear();
        logger.Info("Display Product option {displayChoice} selected", displayChoice);

        if (displayChoice == "1") 
        {
            // Display all products
            var db = new DataContext();
            var products = db.Products.OrderBy(p => p.ProductName);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{products.Count()} products returned");
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var product in products)
            {
                if (product.Discontinued)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{product.ProductName} (Discontinued)");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"{product.ProductName} (Active)");
                }
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
        else if (displayChoice == "2")
        {
            // Display all active products
            var db = new DataContext();
            var products = db.Products.Where(p => !p.Discontinued).OrderBy(p => p.ProductName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{products.Count()} active products returned");
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var product in products)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"{product.ProductName} (Active)");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
        else if (displayChoice == "3")
        {
            // Display all discontinued products
            var db = new DataContext();
            var products = db.Products.Where(p => p.Discontinued).OrderBy(p => p.ProductName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{products.Count()} discontinued products returned");
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var product in products)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{product.ProductName} (Discontinued)");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
        else if (String.IsNullOrEmpty(displayChoice))
        {
            // Return to Products menu
        }
    }
    else if (choice == "4")
    {
        //choose a Specific Product to display
        var db = new DataContext();
        var products = db.Products.OrderBy(p => p.ProductId);
        foreach (var p in products)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{p.ProductId}) {p.ProductName}");
        }
        Console.WriteLine("Enter the Product ID to display:");
        int productId = int.Parse(Console.ReadLine()!);

        //display all records of selected product
        Product? product = db.Products.FirstOrDefault(p => p.ProductId == productId);
         if (product == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Product with ID {productId} not found.");
            Console.ForegroundColor = ConsoleColor.White;
            logger.Error($"Product with ID {productId} not found");
        }
        else
        {
            Console.Clear();
            logger.Info($"Product {productId} - {product.ProductName} selected for display");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Product ID: {product.ProductId}");
            Console.WriteLine($"Product Name: {product.ProductName}");
            Console.WriteLine($"Category ID: {product.CategoryId}");
            Console.WriteLine($"Unit Price: {product.UnitPrice:C}");
            Console.WriteLine($"Units In Stock: {product.UnitsInStock}");
            Console.WriteLine($"Discontinued: {product.Discontinued}");
            Console.ForegroundColor = ConsoleColor.White;
    }
    }
    } 
    else if (mainChoice == "3")
    {
        // Delete submenu
        Console.WriteLine("Delete Menu:");
        Console.WriteLine("(Delete options to be implemented)");
        Console.WriteLine("Enter to return to main menu");
        string? choice = Console.ReadLine();
        Console.Clear();
    }
    else if (String.IsNullOrEmpty(mainChoice))
    {
        break;
    }
    Console.WriteLine();
} while (true);

logger.Info("Program ended");