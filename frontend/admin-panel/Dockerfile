FROM node:22-alpine AS build
WORKDIR /src
COPY frontend/admin-panel/package*.json ./
RUN npm install
COPY frontend/admin-panel/ ./
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /src/dist/admin-panel/browser /usr/share/nginx/html
COPY frontend/admin-panel/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 4201
