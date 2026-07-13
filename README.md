# CutyPAI

CutyPAI is an AI companion platform with:

- **ASP.NET Core 9 backend** (MVC + REST API)
- **MongoDB** for persistence
- **JWT + Cookie authentication**
- **OpenAI chat integration**
- **AWS Polly text-to-speech**
- **Rhubarb lip-sync generation**
- **Next.js frontend** (in `clientApp`) with a 3D avatar/chat experience

---

## Project Structure

- `/Controllers`, `/Services`, `/Repositories`, `/Models` – backend application logic
- `/Views` – ASP.NET MVC views
- `/wwwroot` – static assets
- `/Styles` – Tailwind source for backend UI styles
- `/clientApp` – Next.js frontend app

---

## Tech Stack

### Backend
- .NET 9
- ASP.NET Core MVC + Web API
- MongoDB Driver
- OpenAI SDK
- AWS Polly SDK
- Serilog

### Frontend
- Next.js 15
- React 19
- Tailwind CSS 4
- Three.js / React Three Fiber

---

## Prerequisites

- **.NET SDK 9.0+**
- **Node.js 18+** and npm
- **MongoDB**
- (Optional but recommended) **ffmpeg**
- **Rhubarb Lip Sync** binary available at configured path

---
