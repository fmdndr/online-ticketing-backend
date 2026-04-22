# Kubernetes / Rancher Deployment Guide

## Overview

This directory contains Kustomize-based Kubernetes manifests for deploying the full Event Ticketing System to a Rancher (RKE2/K3s) cluster.

### Architecture
```
k8s/
├── base/                   # Shared manifests (all environments inherit these)
│   ├── namespace.yaml
│   ├── configmap.yaml      # Non-secret env vars
│   ├── secret.yaml         # Sensitive credentials (placeholders — replace in prod)
│   ├── postgres.yaml       # PostgreSQL StatefulSet + Service
│   ├── mongo.yaml          # MongoDB StatefulSet + Service
│   ├── redis.yaml          # Redis Deployment + Service + PVC
│   ├── rabbitmq.yaml       # RabbitMQ StatefulSet + Service
│   ├── seq.yaml            # Seq logging Deployment + Service + PVC
│   ├── catalog-deployment.yaml / catalog-service.yaml
│   ├── basket-deployment.yaml  / basket-service.yaml
│   ├── payment-deployment.yaml / payment-service.yaml
│   ├── gateway-deployment.yaml / gateway-service.yaml
│   ├── ingress.yaml        # NGINX Ingress → gateway-api
│   └── kustomization.yaml
└── overlays/
    ├── dev/                # ticketing-dev namespace, dev-latest tags, 1 replica
    └── prod/               # ticketing-prod namespace, prod-latest tags, 2 replicas + HPA
```

## Prerequisites

1. **kubectl** installed and pointing to your Rancher cluster
2. **kustomize** v5+ (or use `kubectl apply -k`)
3. NGINX Ingress controller installed in the cluster
4. *(Optional)* cert-manager + ClusterIssuer `letsencrypt-prod` for automatic TLS

## Before First Deploy

### 1. Replace placeholder values

In `base/catalog-deployment.yaml`, `basket-deployment.yaml`, `payment-deployment.yaml`, `gateway-deployment.yaml`:
```
YOUR_DOCKERHUB_USERNAME  →  your actual DockerHub username
```

In `base/ingress.yaml` and overlay kustomization files:
```
api.your-ticketing-domain.com  →  your actual domain
```

### 2. Create DockerHub pull secret in the cluster

```bash
kubectl create namespace online-ticketing-backend

# Create pull secret in each namespace (DockerHub public repos don't need this,
# but it prevents rate-limiting on pulls)
kubectl create secret docker-registry dockerhub-pull-secret \
  --docker-server=https://index.docker.io/v1/ \
  --docker-username=YOUR_DOCKERHUB_USERNAME \
  --docker-password=YOUR_DOCKERHUB_TOKEN \
  -n online-ticketing-backend
```

> **Note**: If your DockerHub repositories are **public**, you can remove the `imagePullSecrets` section from each deployment and skip this step entirely.

### 3. Update production secrets

Edit `base/secret.yaml` and set real passwords **or** use Rancher UI to manage secrets directly (recommended for production).

## Manual Deploy

```bash
# Deploy to prod
kubectl apply -k k8s/overlays/prod

# Check status
kubectl get all -n online-ticketing-backend
```

## CI/CD (GitHub Actions)

The workflow in `.github/workflows/deploy.yml` automatically:
1. Builds all 4 Docker images in parallel
2. Pushes them to DockerHub with `{env}-{sha}` tags
3. Updates image tags in kustomization
4. Applies the correct overlay to the cluster
5. Waits for all rollouts to complete

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `DOCKERHUB_USERNAME` | Your DockerHub username |
| `DOCKERHUB_TOKEN` | DockerHub access token (Account Settings → Security → New Access Token) |
| `KUBE_CONFIG` | Base64-encoded kubeconfig: `cat ~/.kube/config \| base64 \| tr -d '\n'` |

### Branch → Environment mapping

| Branch | Namespace | Image Tag |
|--------|-----------|-----------|
| `dev` | `ticketing-dev` | `dev-{sha}` |
| `main` / `prod` | `ticketing-prod` | `prod-{sha}` |

## Service URLs (inside cluster)

| Service | Internal URL |
|---------|-------------|
| Gateway (entry point) | `http://gateway-api:8080` |
| Catalog | `http://catalog-api:8080` |
| Basket | `http://basket-api:8080` |
| Payment | `http://payment-api:8080` |
| PostgreSQL | `postgres:5432` |
| MongoDB | `mongo:27017` |
| Redis | `redis:6379` |
| RabbitMQ AMQP | `rabbitmq:5672` |
| RabbitMQ Management | `rabbitmq:15672` |
| Seq Ingestion | `seq:5341` |
| Seq UI | `seq:80` |

## Swagger UIs (port-forward for debugging)

```bash
kubectl port-forward svc/catalog-api 5001:8080 -n online-ticketing-backend &
kubectl port-forward svc/basket-api  5002:8080 -n online-ticketing-backend &
kubectl port-forward svc/payment-api 5003:8080 -n online-ticketing-backend &
kubectl port-forward svc/gateway-api 5010:8080 -n online-ticketing-backend &
kubectl port-forward svc/seq         8081:80   -n online-ticketing-backend &
```

Then open:
- Gateway: http://localhost:5010
- Catalog Swagger: http://localhost:5001/swagger
- Basket Swagger: http://localhost:5002/swagger
- Payment Swagger: http://localhost:5003/swagger
- Seq UI: http://localhost:8081 (admin / admin123!)

