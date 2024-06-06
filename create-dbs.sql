IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'link-audit') CREATE DATABASE [link-audit];
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'link-census') CREATE DATABASE [link-census];
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'link-dataacquisition') CREATE DATABASE [link-dataacquisition];
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'link-normalization') CREATE DATABASE [link-normalization];
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'link-notification') CREATE DATABASE [link-notification];
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'link-tenant') CREATE DATABASE [link-tenant];
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'link-validation') CREATE DATABASE [link-validation];
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'link-querydispatch') CREATE DATABASE [link-querydispatch];