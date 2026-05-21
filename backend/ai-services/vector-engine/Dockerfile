FROM python:3.12-slim
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends build-essential \
    && rm -rf /var/lib/apt/lists/*
COPY backend/ai-services/vector-engine/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY backend/ai-services/vector-engine/app ./app
EXPOSE 8103
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8103"]
