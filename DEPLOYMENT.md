# Deployment Guide

## Azure App Service Deployment

### Step 1: Prepare for Deployment

```bash
# Build the application
dotnet publish -c Release -o ./publish

# Verify all files are present
ls -la publish/
```

### Step 2: Create App Service Resources

```bash
# Create resource group
az group create --name rg-document-agent --location eastus

# Create App Service plan
az appservice plan create \
  --name plan-document-agent \
  --resource-group rg-document-agent \
  --sku B2 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group rg-document-agent \
  --plan plan-document-agent \
  --name app-document-agent \
  --runtime "DOTNET|8.0"
```

### Step 3: Deploy Application

#### Using Azure CLI

```bash
az webapp deployment source config-zip \
  --resource-group rg-document-agent \
  --name app-document-agent \
  --src publish.zip
```

#### Using Visual Studio

1. Right-click project → Publish
2. Select "Azure"
3. Choose "App Service"
4. Create new or select existing
5. Click Publish

### Step 4: Configure Application Settings

```bash
az webapp config appsettings set \
  --resource-group rg-document-agent \
  --name app-document-agent \
  --settings \
    "Azure__Storage__ConnectionString=your-connection-string" \
    "Azure__OpenAI__Endpoint=https://your-resource.openai.azure.com/" \
    "Azure__OpenAI__ApiKey=your-api-key" \
    "Azure__OpenAI__DeploymentName=gpt-4"
```

## Docker Deployment

### Build Docker Image

```bash
docker build -t document-agent:latest .
```

### Push to Container Registry

```bash
# Create container registry
az acr create \
  --resource-group rg-document-agent \
  --name acrDocumentAgent \
  --sku Basic

# Login to registry
az acr login --name acrDocumentAgent

# Tag image
docker tag document-agent:latest acrDocumentAgent.azurecr.io/document-agent:latest

# Push image
docker push acrDocumentAgent.azurecr.io/document-agent:latest
```

### Deploy to Container Instances

```bash
az container create \
  --resource-group rg-document-agent \
  --name document-agent \
  --image acrDocumentAgent.azurecr.io/document-agent:latest \
  --cpu 2 \
  --memory 4 \
  --registry-login-server acrDocumentAgent.azurecr.io \
  --registry-username username \
  --registry-password password \
  --environment-variables \
    "Azure__Storage__ConnectionString=your-connection-string" \
    "Azure__OpenAI__Endpoint=https://your-resource.openai.azure.com/" \
    "Azure__OpenAI__ApiKey=your-api-key"
```

## GitHub Actions CI/CD

### 1. Set Up Secrets

In GitHub repository settings, add secrets:

- `AZURE_PUBLISH_PROFILE` - App Service publish profile
- `AZURE_STORAGE_CONNECTION_STRING`
- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_API_KEY`

### 2. Create Workflow File

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: app-document-agent
        publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
        package: ./publish
```

## Kubernetes Deployment

### Create Deployment Manifest

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: document-agent
spec:
  replicas: 3
  selector:
    matchLabels:
      app: document-agent
  template:
    metadata:
      labels:
        app: document-agent
    spec:
      containers:
      - name: document-agent
        image: acrDocumentAgent.azurecr.io/document-agent:latest
        ports:
        - containerPort: 5000
        env:
        - name: Azure__Storage__ConnectionString
          valueFrom:
            secretKeyRef:
              name: azure-credentials
              key: storage-connection-string
        - name: Azure__OpenAI__Endpoint
          valueFrom:
            secretKeyRef:
              name: azure-credentials
              key: openai-endpoint
        - name: Azure__OpenAI__ApiKey
          valueFrom:
            secretKeyRef:
              name: azure-credentials
              key: openai-api-key
        resources:
          limits:
            cpu: "500m"
            memory: "512Mi"
          requests:
            cpu: "250m"
            memory: "256Mi"
---
apiVersion: v1
kind: Service
metadata:
  name: document-agent-service
spec:
  selector:
    app: document-agent
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
  type: LoadBalancer
```

### Deploy to AKS

```bash
# Create secret
kubectl create secret generic azure-credentials \
  --from-literal=storage-connection-string='your-connection-string' \
  --from-literal=openai-endpoint='your-endpoint' \
  --from-literal=openai-api-key='your-key'

# Apply deployment
kubectl apply -f deployment.yaml

# Check status
kubectl get pods
kubectl get svc
```

## Monitoring and Logging

### Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app document-agent \
  --location eastus \
  --resource-group rg-document-agent

# Link to App Service
az webapp config appsettings set \
  --resource-group rg-document-agent \
  --name app-document-agent \
  --settings "ApplicationInsightsAgent_EXTENSION_VERSION=~3"
```

### View Logs

```bash
# Stream application logs
az webapp log tail --resource-group rg-document-agent --name app-document-agent

# Download logs
az webapp log download \
  --resource-group rg-document-agent \
  --name app-document-agent
```

## Performance Optimization

### Enable Caching

1. Set appropriate cache headers
2. Use Azure Redis Cache for session data
3. Implement response compression

### Scaling

```bash
# Auto-scale configuration
az monitor autoscale create \
  --resource-group rg-document-agent \
  --name autoscale-document-agent \
  --resource-name-prefix app-document-agent \
  --resource-type "Microsoft.Web/serverfarms" \
  --min-count 2 \
  --max-count 10
```

## Troubleshooting

### Check Application Logs

```bash
az webapp log show \
  --resource-group rg-document-agent \
  --name app-document-agent
```

### Common Issues

**502 Bad Gateway**: Check Application Insights for startup errors

**Azure Storage Connection Failed**: Verify credentials and firewall

**High Latency**: Review Application Insights performance metrics

## Backup and Recovery

```bash
# Create backup
az webapp deployment slot create \
  --resource-group rg-document-agent \
  --name app-document-agent \
  --slot staging

# Swap slots
az webapp deployment slot swap \
  --resource-group rg-document-agent \
  --name app-document-agent \
  --slot staging
```
