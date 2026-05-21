FROM node:22-alpine AS build
WORKDIR /src
COPY frontend/analytics-dashboard/package*.json ./
RUN npm install
COPY frontend/analytics-dashboard/ ./
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /src/dist/analytics-dashboard/browser /usr/share/nginx/html
COPY frontend/analytics-dashboard/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 4203
