FROM node:22-alpine AS build
WORKDIR /src
COPY frontend/realtime-monitor/package*.json ./
RUN npm install
COPY frontend/realtime-monitor/ ./
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /src/dist/realtime-monitor/browser /usr/share/nginx/html
COPY frontend/realtime-monitor/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 4202
