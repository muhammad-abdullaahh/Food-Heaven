USE FoodHeavenDb;
GO

-- 0. Clear child tables first
DELETE FROM OrderItems;

-- 1. Temporarily drop the Foreign Key so we can use TRUNCATE
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OrderItems_MenuItems')
    ALTER TABLE OrderItems DROP CONSTRAINT FK_OrderItems_MenuItems;

-- 2. TRUNCATE the table (this deletes all rows and PERMANENTLY resets identity to 1)
TRUNCATE TABLE MenuItems;

-- 3. Re-add the Foreign Key
ALTER TABLE OrderItems ADD CONSTRAINT FK_OrderItems_MenuItems 
    FOREIGN KEY (MenuItemId) REFERENCES MenuItems(Id);
GO

-- ============================================================
-- INSERT MENU ITEMS (DML)
-- ============================================================
INSERT INTO MenuItems (Name, Description, Price, Category, ImageUrl, Rating, ReviewCount, IsAvailable, CreatedAt)
VALUES
-- Starters
('Garlic Bread Supreme', 'Toasted baguette slices topped with rich garlic butter and melted mozzarella cheese.', 5.99, 'Starters', '/images/garlic_bread.jpg', 4.6, 120, 1, GETDATE()),
('Crispy Spring Rolls', 'Crispy golden rolls filled with spiced vegetables, served with a sweet chili dip.', 6.99, 'Starters', 'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=800&q=80', 4.2, 85, 1, GETDATE()),
('Spicy Chicken Wings', 'Juicy chicken wings tossed in our signature spicy buffalo sauce.', 9.99, 'Starters', 'https://images.unsplash.com/photo-1527477396000-e27163b481c2?w=800&q=80', 4.8, 310, 1, GETDATE()),

-- Mains
('Zinger Burger', 'Crispy fried chicken fillet topped with lettuce, mayo, and cheese in a seeded bun.', 11.99, 'Mains', 'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=800&q=80', 4.7, 450, 1, GETDATE()),
('Grilled Chicken Steak', 'Juicy grilled chicken breast served with steamed vegetables and mashed potatoes.', 18.99, 'Mains', 'https://images.unsplash.com/photo-1532550907401-a500c9a57435?w=800&q=80', 4.5, 200, 1, GETDATE()),
('Margherita Pizza', 'Classic tomato base topped with fresh mozzarella and basil leaves.', 11.99, 'Mains', 'https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=800&q=80', 4.3, 150, 1, GETDATE()),
('Spaghetti Carbonara', 'Traditional Italian pasta with pancetta, egg, and parmesan cheese sauce.', 14.99, 'Mains', 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=800&q=80', 4.6, 180, 1, GETDATE()),
('Pan-Seared Salmon', 'Atlantic salmon with lemon butter sauce and grilled asparagus.', 28.00, 'Mains', 'https://images.unsplash.com/photo-1560717845-968823efbee1?w=800&q=80', 4.9, 130, 1, GETDATE()),
('Truffle Risotto', 'Creamy arborio rice with fresh black truffles and parmesan crisp.', 24.00, 'Mains', 'https://images.unsplash.com/photo-1476124369491-e7addf5db371?w=800&q=80', 4.7, 210, 1, GETDATE()),

-- Desi
('Chicken Biryani', 'Aromatic basmati rice cooked with spiced chicken and herbs.', 13.99, 'Desi', '/images/biryani.jpg', 4.8, 550, 1, GETDATE()),
('Mutton Karahi', 'Tender mutton cooked in a wok with tomatoes and spices.', 17.99, 'Desi', '/images/karahi.jpg', 4.7, 340, 1, GETDATE()),
('Butter Chicken', 'Chicken simmered in a creamy butter tomato sauce.', 15.99, 'Desi', '/images/butter_chicken.jpg', 4.9, 600, 1, GETDATE()),

-- BBQ
('Afghan Lamb Chops', 'Tender lamb chops marinated in Afghan spices and char-grilled.', 24.99, 'BBQ', '/images/bbq/lamb_chops.jpg', 4.9, 150, 1, GETDATE()),
('Balochi Sajji', 'Traditional Balochi style whole chicken roasted to perfection.', 19.99, 'BBQ', '/images/bbq/balochi_sajji.jpg', 4.8, 120, 1, GETDATE()),
('Bihari Kabab', 'Thin strips of beef marinated in a spicy yogurt mixture and grilled.', 14.99, 'BBQ', '/images/bbq/bihari_kabab.jpg', 4.7, 95, 1, GETDATE()),
('Chapli Kabab', 'Peshawari style minced meat patty with herbs and spices.', 12.99, 'BBQ', '/images/bbq/chapli_kabab.jpg', 4.9, 210, 1, GETDATE()),
('Chicken Malai Boti', 'Creamy chicken chunks marinated in cream and mild spices.', 13.99, 'BBQ', '/images/bbq/malai_boti.jpg', 4.8, 180, 1, GETDATE()),
('Chicken Tikka', 'Spicy grilled chicken leg quarter.', 8.99, 'BBQ', '/images/bbq/chicken_tikka.jpg', 4.6, 300, 1, GETDATE()),
('Fish Tikka', 'Grilled fish fillets marinated in tandoori spices.', 16.99, 'BBQ', '/images/bbq/fish_tikka.jpg', 4.5, 80, 1, GETDATE()),
('Hariyali Boti', 'Chicken chunks marinated in a green herb and spice mixture.', 13.99, 'BBQ', '/images/bbq/hariyali_boti.jpg', 4.7, 65, 1, GETDATE()),
('Kalmi Kabab', 'Chicken drumsticks marinated in a rich spicy mixture and grilled.', 11.99, 'BBQ', '/images/bbq/kalmi_kabab.jpg', 4.6, 90, 1, GETDATE()),
('Kasturi Boti', 'Chicken pieces marinated with cashew paste and saffron.', 15.99, 'BBQ', '/images/bbq/kasturi_boti.jpg', 4.8, 55, 1, GETDATE()),
('Lamb Ribs', 'Succulent lamb ribs seasoned with sea salt and black pepper.', 28.99, 'BBQ', '/images/bbq/lamb_ribs.jpg', 4.9, 110, 1, GETDATE()),
('Mutton Ribs', 'Tender mutton ribs marinated in spicy yogurt and char-grilled.', 26.99, 'BBQ', '/images/bbq/mutton_ribs.jpg', 4.8, 85, 1, GETDATE()),
('Reshmi Kabab', 'Silky smooth minced chicken skewers.', 12.99, 'BBQ', '/images/bbq/reshmi_kabab.jpg', 4.7, 140, 1, GETDATE()),
('Seekh Kabab (Beef)', 'Minced beef skewers with traditional spices.', 10.99, 'BBQ', '/images/bbq/beef_seekh.jpg', 4.7, 250, 1, GETDATE()),
('Seekh Kabab (Chicken)', 'Minced chicken skewers with fresh herbs.', 10.99, 'BBQ', '/images/bbq/chicken_seekh.jpg', 4.6, 190, 1, GETDATE()),
('Tangdi Kabab', 'Spiced chicken drumsticks grilled in a clay oven.', 11.99, 'BBQ', '/images/bbq/tangdi_kabab.jpg', 4.5, 115, 1, GETDATE()),
('Tandoori Platter', 'A mix of tikka, seekh, and malai boti.', 35.99, 'BBQ', '/images/bbq/platter.jpg', 4.9, 450, 1, GETDATE()),
('White Karahi (BBQ)', 'BBQ chicken finished in a creamy white gravy.', 18.99, 'BBQ', '/images/bbq/white_karahi.jpg', 4.8, 130, 1, GETDATE()),
('Garlic Tikka', 'Chicken pieces with a heavy garlic and lemon marinade.', 13.99, 'BBQ', '/images/bbq/garlic_tikka.jpg', 4.6, 70, 1, GETDATE()),
('Achari Tikka', 'Pickle-flavored grilled chicken.', 13.99, 'BBQ', '/images/bbq/achari_tikka.jpg', 4.7, 85, 1, GETDATE()),
('Gola Kabab', 'Small round melt-in-mouth beef kebabs.', 14.99, 'BBQ', '/images/bbq/gola_kabab.jpg', 4.8, 60, 1, GETDATE()),
('Dhaka Chicken', 'Sesame-crusted spicy grilled chicken strips.', 12.99, 'BBQ', '/images/bbq/dhaka_chicken.jpg', 4.5, 105, 1, GETDATE()),
('Paneer Tikka', 'Grilled cottage cheese cubes with peppers.', 11.99, 'BBQ', '/images/bbq/paneer_tikka.jpg', 4.4, 45, 1, GETDATE()),
('Mushroom Tikka', 'Spicy grilled fresh mushrooms.', 10.99, 'BBQ', '/images/bbq/mushroom_tikka.jpg', 4.3, 35, 1, GETDATE()),
('Prawn Tikka', 'King prawns marinated in tandoori spices.', 22.99, 'BBQ', '/images/bbq/prawn_tikka.jpg', 4.7, 75, 1, GETDATE()),
('Chicken 65 (BBQ)', 'South Indian style spicy grilled chicken.', 12.99, 'BBQ', '/images/bbq/chicken65.jpg', 4.6, 110, 1, GETDATE()),
('Lucknowi Galauti Kabab', 'Ultra-soft minced meat kebabs with secret spices.', 16.99, 'BBQ', '/images/bbq/galauti.jpg', 4.9, 95, 1, GETDATE()),
('Shami Kabab', 'Traditional minced meat and lentil patties.', 9.99, 'BBQ', '/images/bbq/shami.jpg', 4.5, 160, 1, GETDATE()),
('Namkeen Tikka', 'Peshawari style salt-only mutton cubes.', 18.99, 'BBQ', '/images/bbq/namkeen.jpg', 4.8, 140, 1, GETDATE()),
('Kastoori Chicken', 'Chicken coated in egg and grilled with spices.', 14.99, 'BBQ', '/images/bbq/kastoori.jpg', 4.7, 50, 1, GETDATE()),
('Afghani Tikka', 'Mildly spiced chicken chunks with yogurt and black pepper.', 13.99, 'BBQ', '/images/bbq/afghani_tikka.jpg', 4.7, 80, 1, GETDATE()),
('Sultani Seekh', 'Extra-long beef skewers with special spices.', 15.99, 'BBQ', '/images/bbq/sultani.jpg', 4.8, 40, 1, GETDATE()),
('Shish Taouk', 'Lebanese style grilled chicken skewers.', 14.99, 'BBQ', '/images/bbq/shish_taouk.jpg', 4.6, 120, 1, GETDATE()),
('Lamb Kofta', 'Middle Eastern spiced lamb meatballs grilled.', 15.99, 'BBQ', '/images/bbq/lamb_kofta.jpg', 4.5, 90, 1, GETDATE()),
('Beef Steak (BBQ)', 'Char-grilled prime beef steak.', 29.99, 'BBQ', '/images/bbq/beef_steak.jpg', 4.8, 70, 1, GETDATE()),
('Turkey Tikka', 'Smoky grilled turkey pieces.', 25.99, 'BBQ', '/images/bbq/turkey.jpg', 4.4, 25, 1, GETDATE()),
('Duck Tikka', 'Rich grilled duck breast with honey glaze.', 32.99, 'BBQ', '/images/bbq/duck.jpg', 4.7, 30, 1, GETDATE()),
('Quail Roast (BBQ)', 'Spiced whole quails grilled.', 18.99, 'BBQ', '/images/bbq/quail.jpg', 4.6, 45, 1, GETDATE()),
('Fish Seekh Kabab', 'Minced fish skewers with mild herbs.', 14.99, 'BBQ', '/images/bbq/fish_seekh.jpg', 4.3, 55, 1, GETDATE()),
('Lobster Tail (BBQ)', 'Grilled lobster tail with garlic butter.', 45.00, 'BBQ', '/images/bbq/lobster.jpg', 4.9, 60, 1, GETDATE()),
('Crab Legs (BBQ)', 'Smoky grilled crab legs with lemon.', 38.00, 'BBQ', '/images/bbq/crab.jpg', 4.8, 40, 1, GETDATE()),
('Stuffed Chicken Breast', 'BBQ chicken breast stuffed with cheese and herbs.', 16.99, 'BBQ', '/images/bbq/stuffed_chicken.jpg', 4.7, 75, 1, GETDATE()),
('Chicken Wings (Hot)', 'Extra spicy char-grilled wings.', 11.99, 'BBQ', '/images/bbq/hot_wings.jpg', 4.6, 220, 1, GETDATE()),
('Chicken Wings (Honey)', 'Sweet and smoky grilled wings.', 11.99, 'BBQ', '/images/bbq/honey_wings.jpg', 4.5, 180, 1, GETDATE()),
('Mutton Chops (Masala)', 'Spicy marinated mutton chops.', 22.99, 'BBQ', '/images/bbq/mutton_chops.jpg', 4.8, 110, 1, GETDATE()),
('Tandoori Chicken (Whole)', 'Whole chicken marinated and roasted.', 24.99, 'BBQ', '/images/bbq/whole_tandoori.jpg', 4.9, 190, 1, GETDATE()),
('Boti Platter (Small)', 'Assorted tikka and malai boti.', 22.00, 'BBQ', '/images/bbq/boti_platter.jpg', 4.7, 100, 1, GETDATE()),
('Kabab Platter (Small)', 'Assorted reshmi and seekh kababs.', 20.00, 'BBQ', '/images/bbq/kabab_platter.jpg', 4.6, 85, 1, GETDATE()),
('Seafood Platter (BBQ)', 'Assorted fish, prawn, and lobster.', 55.00, 'BBQ', '/images/bbq/seafood_platter.jpg', 4.9, 70, 1, GETDATE()),
('Family BBQ Feast', 'A massive platter for the whole family.', 75.00, 'BBQ', '/images/bbq/family_feast.jpg', 5.0, 150, 1, GETDATE()),

-- Healthy
('Chicken Caesar Salad', 'Fresh romaine lettuce, croutons, parmesan, and grilled chicken.', 11.99, 'Healthy', 'https://images.unsplash.com/photo-1550304943-4f24f54ddde9?w=800&q=80', 4.4, 90, 1, GETDATE()),
('Quinoa Power Bowl', 'Nutrient-packed quinoa with roasted veggies.', 13.99, 'Healthy', 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800&q=80', 4.8, 120, 1, GETDATE()),

-- Desserts & Ice Cream 
('Triple Chocolate Cake', 'Decadent chocolate cake layers with fudge frosting.', 6.99, 'Desserts', '/images/choc_cake.jpg', 4.9, 440, 1, GETDATE()),
('Classic Tiramisu', 'Coffee-soaked ladyfingers layered with mascarpone cream.', 7.99, 'Desserts', '/images/tiramisu.jpg', 4.7, 190, 1, GETDATE()),
('Vanilla Bean', 'Made with premium vanilla beans.', 4.99, 'Ice Cream', '/images/ic_vanilla.jpg', 4.3, 110, 1, GETDATE()),

-- Drinks
('Virgin Mojito', 'Alcohol-free mint and lime refreshing cocktail.', 5.99, 'Drinks', '/images/mojito.jpg', 4.5, 300, 1, GETDATE()),
('Classic Coca Cola', 'Ice cold classic cola served with lemon.', 2.50, 'Drinks', '/images/coke.jpg', 4.0, 500, 1, GETDATE());

GO
PRINT 'Menu Items Successfully Inserted!';
