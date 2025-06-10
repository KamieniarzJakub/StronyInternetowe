DROP DATABASE gamestore;
CREATE DATABASE gamestore;

USE gamestore;

CREATE TABLE products (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

INSERT INTO products (name) VALUES 
('ARK: Survival Evolved'),
('Rocket League'),
('Minecraft'),
('GTA VI'),
('Sid Meiers Civilization'),
('Death Stranding'),
('Red Dead Redemption'),
('Cities Skylines'),
('Jurassic World Evolution'),
('Pokemon');
