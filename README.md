# C# Learning Repository

A repository documenting our team's C# learning progress, including weekly lab exercises and a final project built with Blazor.

## Team Members

| Name | Student ID |
| ---- | ---------- |
| Trần Thanh Sang | 3124411257 |
| Lưu Gia Huy | 3124411108 |
| Nguyễn Hoàng Ấn | 3124411021 |
| Nguyễn Nhật Minh | 3124411164 |

## Repository Structure

```
.
├── BTNhom/          # Weekly lab exercises (Buoi1 – Buoi12)
│   ├── Buoi1/       # Each session contains multiple tasks (Bai1, Bai2, ...)
│   ├── ...
│   └── Buoi12/
└── DoAn/            # Final project
    └── BlazorApp/   # Blazor Web App (.NET 10)
```

### BTNhom — Lab Exercises

Contains the team's practice assignments organized by session (`Buoi1` through `Buoi12`). Each session folder holds individual exercises (`Bai1`, `Bai2`, ...) covering C# fundamentals such as classes, OOP concepts, and console applications.

### DoAn — Final Project: BlazorApp

A [Blazor](https://blazor.net) Web Application built on **.NET 10** using interactive server rendering. It is a product-catalog style web app.

**Key parts:**

- `Program.cs` — application entry point, configures Razor Components with interactive server render mode
- `Components/` — Razor components: `App.razor`, `Routes.razor`, layouts, pages, and shared UI
- `Models/` — data models (`Product`, `ProductSection`) for the catalog
- `wwwroot/` — static assets (CSS, JS, images)
- `Dockerfile` — multi-stage build for containerized deployment

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (optional, for containerized runs)

### Run the Lab Exercises

Each exercise is a standalone C# file or project. Navigate into a task folder and run it, e.g.:

```bash
cd BTNhom/Buoi1/Bai1
dotnet run HinhTron.cs   # or open in Visual Studio / VS Code and run
```

### Run the Blazor Project

```bash
cd DoAn/BlazorApp
dotnet restore
dotnet run
```

Then open the URL shown in the terminal (default dev port).

### Run with Docker

```bash
cd DoAn/BlazorApp
docker build -t blazorapp .
docker run -p 5092:5092 blazorapp
```

The app listens on port **5092** inside the container.

## License

This repository is created for educational purposes as part of our coursework.
