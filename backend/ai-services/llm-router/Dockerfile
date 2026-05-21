FROM python:3.12-slim
WORKDIR /app
COPY backend/ai-services/llm-router/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY backend/ai-services/llm-router/app ./app
EXPOSE 8101
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8101"]
