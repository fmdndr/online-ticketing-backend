pipeline {
    agent {
        label 'jenkins-agent'
    }

    environment {
        // DockerHub
        DOCKER_REGISTRY  = 'docker.io'
        DOCKERHUB_USER   = 'fmdx'
        FULL_IMAGE_BASE  = "${DOCKER_REGISTRY}/${DOCKERHUB_USER}"

        // Application
        APP_NAME    = 'ticketing'
        APP_VERSION = "${env.BUILD_NUMBER}"
        K8S_NAMESPACE    = 'online-ticketing-backend'

        // DockerHub credentials — bound globally because both Docker Build and Deploy stages need them.
        DOCKERHUB_CREDENTIALS = credentials('dockerhub-login')

        // Docker config (writable location for DockerHub login)
        DOCKER_CONFIG = "${env.WORKSPACE}/.docker"
    }

    options {
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
        timeout(time: 45, unit: 'MINUTES')
        timestamps()
    }

    parameters {
        string(
            name: 'BRANCH',
            defaultValue: 'main',
            description: 'Git branch to build. Set automatically by the GitHub Actions webhook trigger.'
        )
    }

    stages {
        // =====================================================================
        // 1. Checkout
        // =====================================================================
        stage('📋 Checkout') {
            steps {
                script {
                    def targetBranch = (params.BRANCH ?: 'main')
                        .replaceAll('^refs/heads/', '')
                        .replaceAll('^origin/', '')
                        .trim()

                    echo "🔄 Checking out branch: ${targetBranch}"

                    checkout([
                        $class                           : 'GitSCM',
                        branches                         : [[name: "*/${targetBranch}"]],
                        userRemoteConfigs                : scm.userRemoteConfigs,
                        doGenerateSubmoduleConfigurations: false,
                        extensions                       : []
                    ])

                    env.IMAGE_TAG        = "prod-latest"
                    env.GIT_COMMIT_MSG   = sh(returnStdout: true, script: 'git log -1 --pretty=%B').trim()
                    env.GIT_AUTHOR       = sh(returnStdout: true, script: 'git log -1 --pretty=%an').trim()
                    env.GIT_COMMIT_SHORT = sh(returnStdout: true, script: 'git rev-parse --short HEAD').trim()

                    echo "📦 Namespace   : ${env.K8S_NAMESPACE}"
                    echo "🏷  Image tag   : ${env.IMAGE_TAG}"
                    echo "📝 Commit      : ${env.GIT_COMMIT_MSG}"
                    echo "👤 Author      : ${env.GIT_AUTHOR}"
                    echo "🔖 SHA         : ${env.GIT_COMMIT_SHORT}"
                }
            }
        }

        // =====================================================================
        // 2. Build (.NET)
        // =====================================================================
        // The Jenkins agent workspace lives in the container's writable overlay
        // layer — not in a Docker volume mount — so neither -v nor --volumes-from
        // can reach it. We pipe the workspace as a tar stream into the dotnet
        // SDK container via stdin (-i). Files transfer over the Docker socket;
        // no volume mount is needed.
        // =====================================================================
        stage('🏗️ Build') {
            steps {
                sh """
                    tar -C "\${WORKSPACE}" -cf - . | \
                    docker run --rm -i \
                        -e DOTNET_CLI_HOME=/tmp/.dotnet \
                        -e HOME=/tmp \
                        -w /build \
                        mcr.microsoft.com/dotnet/sdk:9.0 \
                        bash -c "tar -xf - 2>/dev/null \
                            && dotnet restore EventTicketingSystem.sln \
                            && dotnet build EventTicketingSystem.sln -c Release --no-restore"
                    echo "✅ Build complete"
                """
            }
        }

        // =====================================================================
        // 3. Test (.NET)
        // =====================================================================
        stage('🧪 Test') {
            steps {
                sh """
                    tar -C "\${WORKSPACE}" -cf - . | \
                    docker run --rm -i \
                        -e DOTNET_CLI_HOME=/tmp/.dotnet \
                        -e HOME=/tmp \
                        -w /build \
                        mcr.microsoft.com/dotnet/sdk:9.0 \
                        bash -c "tar -xf - 2>/dev/null \
                            && dotnet test EventTicketingSystem.sln -c Release --no-build \
                               --logger 'trx;LogFileName=results.trx'" || true
                """
            }
            post {
                always {
                    script {
                        def hasTrx = sh(
                            script: 'find . -name "*.trx" | head -1 | grep -q . && echo true || echo false',
                            returnStdout: true
                        ).trim()
                        if (hasTrx == 'true') {
                            echo "📋 Test results found."
                        } else {
                            echo "⚠️  No test results found (no test projects yet)"
                        }
                    }
                }
            }
        }

        // =====================================================================
        // 4. Docker Build & Push (all 4 services in parallel)
        // =====================================================================
        stage('🐳 Docker Build & Push') {
            steps {
                script {
                    // Authenticate with DockerHub once
                    sh """
                        set -euo pipefail

                        rm -rf "${DOCKER_CONFIG}"
                        mkdir -p "${DOCKER_CONFIG}"

                        AUTH_B64=\$(printf '%s:%s' "\${DOCKERHUB_CREDENTIALS_USR}" "\${DOCKERHUB_CREDENTIALS_PSW}" | base64 | tr -d '\\n')
                        printf '{"auths":{"https://index.docker.io/v1/":{"auth":"%s"}}}\\n' "\${AUTH_B64}" > "${DOCKER_CONFIG}/config.json"
                        echo "🔑 DockerHub auth configured for: \${DOCKERHUB_CREDENTIALS_USR}"
                    """

                    // Build and push all 5 services in parallel
                    def services = [
                        [name: 'catalog',  image: 'ticketing-catalog',  dockerfile: 'src/Services/Catalog/Catalog.API/Dockerfile'],
                        [name: 'basket',   image: 'ticketing-basket',   dockerfile: 'src/Services/Basket/Basket.API/Dockerfile'],
                        [name: 'payment',  image: 'ticketing-payment',  dockerfile: 'src/Services/Payment/Payment.API/Dockerfile'],
                        [name: 'identity', image: 'ticketing-identity', dockerfile: 'src/Services/Identity/Identity.API/Dockerfile'],
                        [name: 'gateway',  image: 'ticketing-gateway',  dockerfile: 'src/Gateway/Gateway.API/Dockerfile'],
                    ]

                    def parallelBuilds = [:]
                    services.each { svc ->
                        def s = svc  // capture for closure
                        parallelBuilds["build-${s.name}"] = {
                            def fullImage  = "${FULL_IMAGE_BASE}/${s.image}"
                            def latestTag  = "${fullImage}:${env.IMAGE_TAG}"
                            def shaTag     = "${fullImage}:prod-${env.GIT_COMMIT_SHORT}"
                            def buildTag   = "${fullImage}:build-${env.APP_VERSION}"

                            sh """
                                set -euo pipefail
                                echo "🏗️  Building ${s.name}..."
                                docker --config "${DOCKER_CONFIG}" build \\
                                    -f "${s.dockerfile}" \\
                                    -t "${latestTag}" \\
                                    -t "${shaTag}" \\
                                    -t "${buildTag}" \\
                                    .

                                echo "⬆️  Pushing ${s.name}..."
                                docker --config "${DOCKER_CONFIG}" push "${latestTag}"
                                docker --config "${DOCKER_CONFIG}" push "${shaTag}"
                                docker --config "${DOCKER_CONFIG}" push "${buildTag}"
                                echo "✅ ${s.name} pushed: ${latestTag}"
                            """
                        }
                    }

                    parallel parallelBuilds

                    // Scrub credentials
                    sh "rm -f '${DOCKER_CONFIG}/config.json'"
                    echo "✅ All 5 service images published to DockerHub."
                }
            }
        }

        // =====================================================================
        // 5. Deploy to Kubernetes (Rancher) via Kustomize
        // =====================================================================
        stage('🚀 Deploy to Kubernetes') {
            steps {
                script {
                    input message: "🚀 Deploy build #${env.APP_VERSION} (${env.GIT_COMMIT_SHORT}) to PRODUCTION?", ok: "Yes, Deploy!"

                    withCredentials([file(credentialsId: 'rancher-kubeconfig', variable: 'KUBECONFIG_CRED')]) {
                        sh """
                            set -eu

                            # ── Writable kubeconfig copy ──────────────────────────────────
                            mkdir -p "\${WORKSPACE}/.tools"
                            KUBECONFIG_COPY="\${WORKSPACE}/.tools/kubeconfig"
                            cp "\${KUBECONFIG_CRED}" "\${KUBECONFIG_COPY}"
                            chmod 600 "\${KUBECONFIG_COPY}"
                            export KUBECONFIG="\${KUBECONFIG_COPY}"

                            # ── Ensure kubectl is available ───────────────────────────────
                            TOOLS_DIR="\${WORKSPACE}/.tools"
                            LOCAL_KUBECTL="\${TOOLS_DIR}/kubectl"

                            if command -v kubectl >/dev/null 2>&1; then
                                KUBECTL="kubectl"
                                echo "✅ System kubectl found"
                            elif [ -x "\${LOCAL_KUBECTL}" ]; then
                                KUBECTL="\${LOCAL_KUBECTL}"
                                echo "✅ Cached kubectl: \${LOCAL_KUBECTL}"
                            else
                                echo "⬇️  Downloading kubectl..."
                                KUBECTL_VER=\$(curl -sL https://dl.k8s.io/release/stable.txt)
                                curl -sLo "\${LOCAL_KUBECTL}" \\
                                    "https://dl.k8s.io/release/\${KUBECTL_VER}/bin/linux/amd64/kubectl"
                                chmod +x "\${LOCAL_KUBECTL}"
                                KUBECTL="\${LOCAL_KUBECTL}"
                                echo "✅ Downloaded kubectl \${KUBECTL_VER}"
                            fi

                            # ── Patch kubeconfig: trust Rancher self-signed TLS ───────────
                            CLUSTER_NAME=\$(\${KUBECTL} config view --minify -o jsonpath='{.clusters[0].name}')
                            \${KUBECTL} config set-cluster "\${CLUSTER_NAME}" --insecure-skip-tls-verify=true
                            echo "🔓 TLS verify disabled for cluster: \${CLUSTER_NAME}"

                            # ── Create namespace ──────────────────────────────────────────
                            \${KUBECTL} create namespace ${env.K8S_NAMESPACE} --dry-run=client -o yaml | \${KUBECTL} apply --validate=false -f -

                            # ── Create / refresh DockerHub pull secret ────────────────────
                            \${KUBECTL} create secret docker-registry dockerhub-pull-secret --namespace=${env.K8S_NAMESPACE} --docker-server=https://index.docker.io/v1/ --docker-username=\${DOCKERHUB_CREDENTIALS_USR} --docker-password=\${DOCKERHUB_CREDENTIALS_PSW} --dry-run=client -o yaml | \${KUBECTL} apply --validate=false -f -

                            # ── Create / refresh RSA key secret (Identity + Gateway JWT) ──
                            \${KUBECTL} create secret generic ticketing-rsa-keys \
                                --namespace=${env.K8S_NAMESPACE} \
                                --from-file=rsa-private.pem=keys/rsa-private.pem \
                                --from-file=rsa-public.pem=keys/rsa-public.pem \
                                --dry-run=client -o yaml | \${KUBECTL} apply --validate=false -f -
                            echo "🔑 RSA key secret created/updated"

                            # ── Update image tags in kustomization to exact SHA ───────────
                            SHA_TAG="prod-${env.GIT_COMMIT_SHORT}"
                            OVERLAY="k8s/overlays/prod"
                            sed -i "s/newTag: prod-latest/newTag: \${SHA_TAG}/g" "\${OVERLAY}/kustomization.yaml"
                            echo "🏷  Image tags set to: \${SHA_TAG}"

                            # ── Preview ───────────────────────────────────────────────────
                            echo ""
                            echo "📋 Kustomize build preview:"
                            \${KUBECTL} kustomize "\${OVERLAY}"

                            # ── Apply ─────────────────────────────────────────────────────
                            echo ""
                            echo "⬆️  Applying overlay to cluster..."
                            # Remove in-cluster postgres (replaced by remote server).
                            # clusterIP is immutable — must delete before recreating headless.
                            \${KUBECTL} delete statefulset postgres -n ${env.K8S_NAMESPACE} --ignore-not-found=true
                            \${KUBECTL} delete svc postgres -n ${env.K8S_NAMESPACE} --ignore-not-found=true

                            # Remove old RabbitMQ resources (replaced by Kafka).
                            # kubectl apply never removes resources dropped from the overlay.
                            \${KUBECTL} delete statefulset rabbitmq -n ${env.K8S_NAMESPACE} --ignore-not-found=true
                            \${KUBECTL} delete svc rabbitmq -n ${env.K8S_NAMESPACE} --ignore-not-found=true
                            \${KUBECTL} delete configmap rabbitmq-config -n ${env.K8S_NAMESPACE} --ignore-not-found=true
                            \${KUBECTL} delete pvc rabbitmq-data-rabbitmq-0 -n ${env.K8S_NAMESPACE} --ignore-not-found=true
                            echo "🧹 RabbitMQ resources removed (if they existed)"

                            # Recreate Kafka Service so clusterIP can be changed to None (headless).
                            # spec.clusterIP is immutable — kubectl apply will fail if the existing
                            # Service has a real VIP and the manifest now sets clusterIP: None.
                            # Also delete the StatefulSet so the new pod picks up enableServiceLinks:false.
                            \${KUBECTL} delete statefulset kafka -n ${env.K8S_NAMESPACE} --ignore-not-found=true
                            \${KUBECTL} delete svc kafka -n ${env.K8S_NAMESPACE} --ignore-not-found=true
                            echo "🧹 Kafka StatefulSet+Service deleted for clean recreation"

                            \${KUBECTL} apply --validate=false -k "\${OVERLAY}"

                            # ── Rolling restart (picks up new image + config) ─────────────
                            echo ""
                            echo "🔄 Rolling restart all services..."
                            for svc in catalog-api basket-api payment-api identity-api gateway-api; do
                                \${KUBECTL} rollout restart deployment/\${svc} -n ${env.K8S_NAMESPACE}
                            done

                            # ── Wait for all rollouts ─────────────────────────────────────
                            echo ""
                            echo "⏳ Waiting for rollouts..."
                            for svc in catalog-api basket-api payment-api identity-api gateway-api; do
                                echo "  ⏳ \${svc}..."
                                \${KUBECTL} rollout status deployment/\${svc} \\
                                    -n ${env.K8S_NAMESPACE} --timeout=180s
                            done
                        """
                    }
                }
            }
        }

        // =====================================================================
        // 6. Verify Deployment
        // =====================================================================
        stage('✅ Verify Deployment') {
            steps {
                withCredentials([file(credentialsId: 'rancher-kubeconfig', variable: 'KUBECONFIG_CRED')]) {
                    sh """
                        set -eu
                        NS=${env.K8S_NAMESPACE}

                        KUBECONFIG_COPY="\${WORKSPACE}/.tools/kubeconfig"
                        if [ ! -f "\${KUBECONFIG_COPY}" ]; then
                            cp "\${KUBECONFIG_CRED}" "\${KUBECONFIG_COPY}"
                            chmod 600 "\${KUBECONFIG_COPY}"
                        fi
                        export KUBECONFIG="\${KUBECONFIG_COPY}"

                        LOCAL_KUBECTL="\${WORKSPACE}/.tools/kubectl"
                        if command -v kubectl >/dev/null 2>&1; then
                            KUBECTL="kubectl"
                        elif [ -x "\${LOCAL_KUBECTL}" ]; then
                            KUBECTL="\${LOCAL_KUBECTL}"
                        else
                            echo "❌ kubectl not found. Deploy stage must have failed to download it."
                            exit 1
                        fi

                        echo "📊 Pod status:"
                        \${KUBECTL} get pods -n \$NS -l app.kubernetes.io/part-of=ticketing

                        echo ""
                        echo "📊 Services:"
                        \${KUBECTL} get svc -n \$NS

                        echo ""
                        echo "📊 Gateway NodePort (NPM target):"
                        \${KUBECTL} get svc gateway-api -n \$NS

                        echo ""
                        echo "📊 HPA:"
                        \${KUBECTL} get hpa -n \$NS

                        echo ""
                        echo "📝 Gateway logs (last 20 lines):"
                        \${KUBECTL} logs -n \$NS -l app.kubernetes.io/name=gateway-api --tail=20 || true

                        echo ""
                        echo "📝 Payment logs (last 20 lines):"
                        \${KUBECTL} logs -n \$NS -l app.kubernetes.io/name=payment-api --tail=20 || true
                    """
                }
            }
        }

        // =====================================================================
        // 7. Cleanup
        // =====================================================================
        stage('🧹 Cleanup') {
            steps {
                sh '''
                    echo "Removing dangling Docker images..."
                    docker image prune -f || true

                    echo "Removing cached kubeconfig copy..."
                    rm -f "${WORKSPACE}/.tools/kubeconfig" || true

                    echo "Removing Docker config..."
                    rm -rf "${DOCKER_CONFIG}" || true
                '''
            }
        }
    }

    post {
        success {
            script {
                echo """
                ╔═══════════════════════════════════════════════════════╗
                ║          ✅ DEPLOYMENT SUCCESSFUL                     ║
                ╚═══════════════════════════════════════════════════════╝

                📦 Images     : docker.io/fmdx/ticketing-{catalog|basket|payment|identity|gateway}
                🏷  Tag        : prod-${env.GIT_COMMIT_SHORT}
                📍 Namespace  : ${env.K8S_NAMESPACE}
                📝 Commit     : ${env.GIT_COMMIT_MSG}
                👤 Author     : ${env.GIT_AUTHOR}
                🔖 Build      : #${env.APP_VERSION}

                📋 Useful kubectl commands:
                  kubectl get pods -n ${env.K8S_NAMESPACE}
                  kubectl logs -n ${env.K8S_NAMESPACE} -l app.kubernetes.io/name=gateway-api -f
                  kubectl logs -n ${env.K8S_NAMESPACE} -l app.kubernetes.io/name=payment-api -f
                  kubectl get hpa -n ${env.K8S_NAMESPACE}

                🌐 Port-forward for local access:
                  kubectl port-forward svc/gateway-api 5010:8080 -n ${env.K8S_NAMESPACE}
                  kubectl port-forward svc/seq          8081:80   -n ${env.K8S_NAMESPACE}
                  kubectl port-forward svc/kafka-ui     8082:8080 -n ${env.K8S_NAMESPACE}
                """
            }
        }
        failure {
            script {
                echo """
                ╔═══════════════════════════════════════════════════════╗
                ║          ❌ DEPLOYMENT FAILED                         ║
                ╚═══════════════════════════════════════════════════════╝

                Namespace : ${env.K8S_NAMESPACE}
                Build     : #${env.APP_VERSION}
                SHA       : ${env.GIT_COMMIT_SHORT}

                Check the logs above for details.
                """
            }
        }
        always {
            // Ensure Docker credentials never linger on the agent
            sh 'rm -f "${WORKSPACE}/.docker/config.json" || true'
        }
    }
}

