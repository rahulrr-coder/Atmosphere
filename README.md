# ☁️ Atmosphere

> A robust, full-stack weather intelligence platform featuring real-time forecasts, AI-driven insights, and user personalization.

**Atmosphere** is a modern, containerized application built to deliver precise weather data and AI-powered recommendations. Version 2.0 introduces a completely overhauled architecture using microservices, Docker orchestration, and a PostgreSQL database.

## 🚀 Tech Stack

* **Frontend:** Vue.js 3 + Vite (TypeScript)
* **Backend:** .NET 10 Web API
* **Database:** PostgreSQL 15
* **Infrastructure:** Docker Compose & Nginx (Reverse Proxy)
* **AI Integration:** Groq, Gemini, Cerebras
* **Data Source:** OpenWeatherMap API

## 🏗️ Architecture (v2.0)

Atmosphere runs on a 3-container architecture orchestrated by Docker Compose:

1.  **`weather-web`**: Nginx container serving the Vue.js frontend.
2.  **`weather-api`**: .NET 10 backend handling logic, AI processing, and third-party API calls.
3.  **`weather-db`**: Persistent PostgreSQL storage for user accounts and preferences.

## 🛠️ Getting Started

### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.
* Git.

### Installation

1.  **Clone the repository**
    ```bash
    git clone [https://github.com/yourusername/atmosphere.git](https://github.com/yourusername/atmosphere.git)
    cd atmosphere
    ```

2.  **Setup Environment Variables**
    Create a `.env` file in the root directory and populate it with your keys (see `.env.example` for reference).
    ```bash
    cp .env.example .env
    ```
    *Open `.env` and fill in your API keys (OpenWeather, Groq, etc.) and Database credentials.*

3.  **Run with Docker**
    Launch the entire stack in detached mode:
    ```bash
    docker compose up -d --remove-orphans
    ```

### 🌍 Access Points

Once the containers are up, access the application at:

* **Frontend (App):** [http://localhost:5173](http://localhost:5173)
* **Backend (Swagger API):** [http://localhost:5160/swagger](http://localhost:5160/swagger)

## 📦 Database & Migrations

The application automatically applies migrations on startup. If you need to access the database manually:
* **Host:** `localhost`
* **Port:** `5432`
* **User/Pass:** (As defined in your `.env` file)

## 🧪 Development

To stop the application:
```bash
docker compose down
```
To view logs (frontend, backend, and db):
```bash
docker compose logs -f
```

📄 License
This project is licensed under the MIT License.