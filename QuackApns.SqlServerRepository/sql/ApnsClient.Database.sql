USE [master]
GO
/****** Object:  Database [ApnsClient]    Script Date: 11/30/2014 10:20:52 PM ******/
CREATE DATABASE [ApnsClient]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'ApnsClient', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\ApnsClient.mdf' , SIZE = 365568KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB ), 
 FILEGROUP [ApnsMod] CONTAINS MEMORY_OPTIMIZED_DATA  DEFAULT
( NAME = N'ApnsClient_Mod', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\ApnsClient_Mod' , MAXSIZE = UNLIMITED)
 LOG ON 
( NAME = N'ApnsClient_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\ApnsClient_log.ldf' , SIZE = 1341184KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [ApnsClient] SET COMPATIBILITY_LEVEL = 120
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [ApnsClient].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [ApnsClient] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [ApnsClient] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [ApnsClient] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [ApnsClient] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [ApnsClient] SET ARITHABORT OFF 
GO
ALTER DATABASE [ApnsClient] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [ApnsClient] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [ApnsClient] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [ApnsClient] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [ApnsClient] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [ApnsClient] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [ApnsClient] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [ApnsClient] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [ApnsClient] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [ApnsClient] SET  DISABLE_BROKER 
GO
ALTER DATABASE [ApnsClient] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [ApnsClient] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [ApnsClient] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [ApnsClient] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO
ALTER DATABASE [ApnsClient] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [ApnsClient] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [ApnsClient] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [ApnsClient] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [ApnsClient] SET  MULTI_USER 
GO
ALTER DATABASE [ApnsClient] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [ApnsClient] SET DB_CHAINING OFF 
GO
ALTER DATABASE [ApnsClient] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [ApnsClient] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO
ALTER DATABASE [ApnsClient] SET DELAYED_DURABILITY = DISABLED 
GO
EXEC sys.sp_db_vardecimal_storage_format N'ApnsClient', N'ON'
GO
ALTER DATABASE [ApnsClient] SET  READ_WRITE 
GO
