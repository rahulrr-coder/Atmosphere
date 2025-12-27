# Stage 1: Build the Vue App
FROM mirror.gcr.io/library/node:20 AS build
WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm install

# Copy source code and build
COPY . .
RUN npm run build

# Stage 2: Serve with Nginx (Production Web Server)
FROM mirror.gcr.io/library/nginx:alpine
WORKDIR /usr/share/nginx/html

# Clear default nginx files
RUN rm -rf ./*

# Copy the 'dist' folder (your built website) from Stage 1
COPY --from=build /app/dist .

# Expose port 80 (Standard Web Port)
EXPOSE 80

# Start Nginx
CMD ["nginx", "-g", "daemon off;"]