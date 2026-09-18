#!/bin/sh
# Install Argo CD into the local k3d cluster and register the TaxiBlitz app.
#   sh argocd/install.sh
set -e
ARGOCD_VERSION=v3.5.3
cd "$(dirname "$0")"

kubectl create namespace argocd --dry-run=client -o yaml | kubectl apply -f -
kubectl apply -n argocd --server-side --force-conflicts \
  -f "https://raw.githubusercontent.com/argoproj/argo-cd/${ARGOCD_VERSION}/manifests/install.yaml"

# serve the UI over plain HTTP inside the cluster; Traefik terminates TLS
kubectl -n argocd patch configmap argocd-cmd-params-cm --type merge -p '{"data":{"server.insecure":"true"}}'
kubectl -n argocd rollout restart deploy/argocd-server
kubectl -n argocd rollout status deploy/argocd-server --timeout=300s

kubectl apply -f ingress.yaml
kubectl apply -f application.yaml

echo
echo "Argo CD UI : https://argocd.127.0.0.1.nip.io"
echo "user       : admin"
printf "password   : "
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath='{.data.password}' | base64 -d; echo
