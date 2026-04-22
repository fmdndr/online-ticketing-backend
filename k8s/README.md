# Kubernetes / Rancher Deployment Guide

## Overview

This directory contains Kustomize-based Kubernetes manifests for deploying the full Event Ticketing System to a Rancher (RKE2/K3s) cluster with **Nginx Proxy Manager (NPM)** handling external routing.

### Architecture
```
k8s/
├── base/                   # Shared manifests (all environments inherit these)
│   ├── namespace.yaml
│   ├── configmap.yaml      # Non-secret env vars
│   ├── secret.yaml.example # Sensitive credentials (placeholders — create in Rancher UI)
│   ├── postgres-external.yaml  # Remote PostgreSQL Service + Endpoints
│   ├── mongo.yaml          # MongoDB StatefulSet + Service
│   ├── redis.yaml          # Redis Deployment + Service + PVC
│   ├── rabbitmq.yaml       # RabbitMQ StatefulSet + Service
│   ├── seq.yaml            # Seq logging Deployment + Service + PVC
│   ├── catalog-deployment.yaml / catalog-service.yaml
│   ├── basket-deployment.yaml  / basket-service.yaml
│   ├── payment-deployment.yaml / payment-service.yaml
│   ├── gateway-deployment.yaml / gateway-service.yaml  (ClusterIP 10.43.240.101)
│   ├── ingress.yaml        # NOT deployed — kept for reference only
│   └── kustomization.yaml
└── overlays/
    └── prod/               # online-ticketing-backend namespace, prod-latest tags, HPA
```

### Traffic Flow
```
Browser ──HTTPS──▶ Cloudflare ──▶ Nginx Proxy Manager (NPM) ──HTTP──▶ K8s ClusterIP (10.43.240.101:80)
                                    (TLS termination)                     │
                                                                    gateway-api pod :8080
                                                                     ├── catalog-api:8080
                                                                     ├── basket-api:8080
                                                                     └── payment-api:8080
```

## Prerequisites

1. **kubectl** installed and pointing to your Rancher cluster
2. **kustomize** v5+ (or use `kubectl apply -k`)
3. **Nginx Proxy Manager** running and accessible (handles TLS + reverse proxy)
4. **Cloudflare** DNS A record pointing `prod.socratic-event.com` to your server's public IP
5. Cloudflare SSL/TLS encryption mode configured (Flexible or Full depending on NPM SSL setup)

## Before First Deploy

### 1. Create namespace and pull secret

```bash
kubectl create namespace online-ticketing-backend

# DockerHub pull secret (prevents rate-limiting on image pulls)
kubectl create secret docker-registry dockerhub-pull-secret \
  --docker-server=https://index.docker.io/v1/ \
  --docker-username=YOUR_DOCKERHUB_USERNAME \
  --docker-password=YOUR_DOCKERHUB_TOKEN \
  -n online-ticketing-backend
```

### 2. Create application secrets (via Rancher UI — recommended)

Create a Secret named `ticketing-secrets` in the `online-ticketing-backend` namespace with these keys:

| Key | Example Value |
|-----|---------------|
| `ConnectionStrings__PaymentDb` | `Host=84.247.134.65;Port=5432;Database=ticketing_payments;Username=...;Password=...` |
| `DatabaseSettings__ConnectionString` | `mongodb://mongo:27017` |

See `base/secret.yaml.example` for the full template.

### 3. Configure Nginx Proxy Manager

Create a **Proxy Host** in NPM:

| Field | Value |
|-------|-------|
| **Domain Names** | `prod.socratic-event.com` |
| **Scheme** | `http` |
| **Forward Hostname / IP** | `10.43.240.101` (Static ClusterIP) |
| **Forward Port** | `80` |
| **Block Common Exploits** | ✅ |
| **Websockets Support** | ✅ (optional) |

**SSL Tab** (if NPM handles TLS):
- Request a new SSL certificate via Let's Encrypt, or
- Use a Cloudflare Origin Certificate
- Enable **Force SSL** and **HTTP/2 Support**

### 4. Cloudflare DNS

Point `prod.socratic-event.com` (A record) to your server's **public IP address** (the machine where NPM is running).

| Setting | Value |
|---------|-------|
| DNS A Record | `prod.socratic-event.com` → `<your-server-public-IP>` |
| Proxy status | 🟠 Proxied (orange cloud) |
| SSL/TLS mode | **Full** (if NPM has SSL) or **Flexible** (if NPM serves HTTP) |

## Manual Deploy

```bash
# Deploy to prod
kubectl apply -k k8s/overlays/prod

# Check status
kubectl get all -n online-ticketing-backend

# Should show TYPE=ClusterIP, CLUSTER-IP=10.43.240.101, PORT(S)=80/TCP
```

## CI/CD (Jenkins)

The `Jenkinsfile` in the repo root automatically:
1. Builds the .NET solution inside a Docker container
2. Builds all 4 Docker images in parallel
3. Pushes to DockerHub with `prod-<sha>` tags
4. Updates image tags in the prod overlay kustomization
5. Applies the overlay to the cluster via the Rancher kubeconfig
6. Rolling restarts all deployments

### Required Jenkins Credentials

| Credential ID | Type | Description |
|---------------|------|-------------|
| `dockerhub-login` | Username/Password | DockerHub username + access token |
| `rancher-kubeconfig` | Secret File | Rancher kubeconfig YAML |

## Service URLs (inside cluster)

| Service | Internal URL |
|---------|-------------|
| Gateway (entry point) | `http://gateway-api:8080` |
| Catalog | `http://catalog-api:8080` |
| Basket | `http://basket-api:8080` |
| Payment | `http://payment-api:8080` |
| PostgreSQL | `postgres:5432` (remote via Endpoints) |
| MongoDB | `mongo:27017` |
| Redis | `redis:6379` |
| RabbitMQ AMQP | `rabbitmq:5672` |
| RabbitMQ Management | `rabbitmq:15672` |
| Seq Ingestion | `seq:5341` |
| Seq UI | `seq:80` |

## Production URLs (via Cloudflare + NPM)

| URL | Description |
|-----|-------------|
| `https://prod.socratic-event.com/` | Gateway health check |
| `https://prod.socratic-event.com/swagger/catalog` | Catalog Swagger UI |
| `https://prod.socratic-event.com/swagger/basket` | Basket Swagger UI |
| `https://prod.socratic-event.com/swagger/payment` | Payment Swagger UI |
| `https://prod.socratic-event.com/api/catalog/...` | Catalog API |
| `https://prod.socratic-event.com/api/basket/...` | Basket API |
| `https://prod.socratic-event.com/api/payment/...` | Payment API |

## Debugging (port-forward)

```bash
kubectl port-forward svc/catalog-api 5001:8080 -n online-ticketing-backend &
kubectl port-forward svc/basket-api  5002:8080 -n online-ticketing-backend &
kubectl port-forward svc/payment-api 5003:8080 -n online-ticketing-backend &
kubectl port-forward svc/gateway-api 5010:80   -n online-ticketing-backend &
kubectl port-forward svc/seq         8081:80   -n online-ticketing-backend &
```

Then open:
- Gateway: http://localhost:5010
- Catalog Swagger: http://localhost:5001/swagger/catalog
- Basket Swagger: http://localhost:5002/swagger/basket
- Payment Swagger: http://localhost:5003/swagger/payment
- Seq UI: http://localhost:8081 (admin / admin123!)
