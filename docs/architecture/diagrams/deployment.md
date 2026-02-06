# Deployment Architecture

## Production Deployment

```mermaid
graph TB
    subgraph "Internet"
        USERS[Users]
    end
    
    subgraph "CDN / Edge"
        CDN[CloudFlare / AWS CloudFront]
    end
    
    subgraph "Load Balancer"
        LB[NGINX / AWS ALB]
    end
    
    subgraph "Application Tier - Auto Scaling Group"
        APP1[App Instance 1<br/>Node.js + TypeScript]
        APP2[App Instance 2<br/>Node.js + TypeScript]
        APP3[App Instance N<br/>Node.js + TypeScript]
    end
    
    subgraph "Cache Layer"
        REDIS_PRIMARY[Redis Primary]
        REDIS_REPLICA[Redis Replica]
    end
    
    subgraph "Storage Layer"
        S3[S3 / Blob Storage<br/>AI Brain Files<br/>Read-Only]
        RDS[PostgreSQL RDS<br/>Audit Logs]
    end
    
    subgraph "External Services"
        OPENAI[OpenAI API]
        ANTHROPIC[Anthropic API]
        AZURE[Azure OpenAI]
    end
    
    subgraph "Monitoring"
        PROM[Prometheus]
        GRAFANA[Grafana]
        ALERTS[AlertManager]
    end
    
    USERS --> CDN
    CDN --> LB
    LB --> APP1
    LB --> APP2
    LB --> APP3
    
    APP1 --> REDIS_PRIMARY
    APP2 --> REDIS_PRIMARY
    APP3 --> REDIS_PRIMARY
    REDIS_PRIMARY --> REDIS_REPLICA
    
    APP1 --> S3
    APP2 --> S3
    APP3 --> S3
    
    APP1 --> RDS
    APP2 --> RDS
    APP3 --> RDS
    
    APP1 --> OPENAI
    APP1 --> ANTHROPIC
    APP1 --> AZURE
    APP2 --> OPENAI
    APP2 --> ANTHROPIC
    APP2 --> AZURE
    APP3 --> OPENAI
    APP3 --> ANTHROPIC
    APP3 --> AZURE
    
    APP1 --> PROM
    APP2 --> PROM
    APP3 --> PROM
    PROM --> GRAFANA
    PROM --> ALERTS
    
    style S3 fill:#e1f5ff
    style REDIS_PRIMARY fill:#fff3e0
    style RDS fill:#e8f5e9
```

## Container Architecture

```mermaid
graph TB
    subgraph "Docker Container - App Instance"
        subgraph "Application"
            NODE[Node.js Runtime]
            APP[PCF Builder App]
        end
        
        subgraph "Mounted Volumes"
            BRAIN_VOL[/ai-brain<br/>Read-Only Mount]
            TEMP_VOL[/tmp<br/>Temporary Files]
        end
        
        APP --> BRAIN_VOL
        APP --> TEMP_VOL
    end
    
    subgraph "External Storage"
        S3[S3 Bucket<br/>AI Brain Source]
    end
    
    S3 -.->|Sync on Deploy| BRAIN_VOL
    
    style BRAIN_VOL fill:#e1f5ff
```

## Kubernetes Deployment

```mermaid
graph TB
    subgraph "Kubernetes Cluster"
        subgraph "Ingress"
            INGRESS[NGINX Ingress Controller]
        end
        
        subgraph "Application Namespace"
            SVC[Service<br/>LoadBalancer]
            
            subgraph "Deployment"
                POD1[Pod 1<br/>App Container]
                POD2[Pod 2<br/>App Container]
                POD3[Pod N<br/>App Container]
            end
            
            HPA[Horizontal Pod Autoscaler]
        end
        
        subgraph "Cache Namespace"
            REDIS_SVC[Redis Service]
            REDIS_MASTER[Redis Master]
            REDIS_SLAVE[Redis Slave]
        end
        
        subgraph "Storage"
            PV[Persistent Volume<br/>AI Brain<br/>ReadOnlyMany]
        end
    end
    
    subgraph "External"
        RDS[AWS RDS<br/>PostgreSQL]
        S3[S3 Bucket<br/>AI Brain]
    end
    
    INGRESS --> SVC
    SVC --> POD1
    SVC --> POD2
    SVC --> POD3
    
    HPA -.->|scales| POD1
    HPA -.->|scales| POD2
    HPA -.->|scales| POD3
    
    POD1 --> REDIS_SVC
    POD2 --> REDIS_SVC
    POD3 --> REDIS_SVC
    REDIS_SVC --> REDIS_MASTER
    REDIS_MASTER --> REDIS_SLAVE
    
    POD1 --> PV
    POD2 --> PV
    POD3 --> PV
    
    POD1 --> RDS
    POD2 --> RDS
    POD3 --> RDS
    
    S3 -.->|init| PV
    
    style PV fill:#e1f5ff
    style REDIS_MASTER fill:#fff3e0
    style RDS fill:#e8f5e9
```

## CI/CD Pipeline

```mermaid
flowchart LR
    subgraph "Source Control"
        GIT[Git Repository]
    end
    
    subgraph "CI Pipeline"
        BUILD[Build<br/>npm install<br/>npm run build]
        TEST[Test<br/>Unit Tests<br/>Integration Tests]
        LINT[Lint<br/>ESLint<br/>TypeScript Check]
    end
    
    subgraph "Artifact Creation"
        DOCKER[Build Docker Image]
        PUSH[Push to Registry]
    end
    
    subgraph "CD Pipeline"
        DEPLOY_STAGING[Deploy to Staging]
        SMOKE[Smoke Tests]
        DEPLOY_PROD[Deploy to Production]
    end
    
    GIT --> BUILD
    BUILD --> TEST
    TEST --> LINT
    LINT --> DOCKER
    DOCKER --> PUSH
    PUSH --> DEPLOY_STAGING
    DEPLOY_STAGING --> SMOKE
    SMOKE -->|Pass| DEPLOY_PROD
    SMOKE -->|Fail| ROLLBACK[Rollback]
    
    style DEPLOY_PROD fill:#c8e6c9
    style ROLLBACK fill:#ffcdd2
```

## High Availability Setup

```mermaid
graph TB
    subgraph "Region 1 - Primary"
        LB1[Load Balancer]
        APP1A[App Instance 1A]
        APP1B[App Instance 1B]
        REDIS1[Redis Primary]
        RDS1[RDS Primary]
    end
    
    subgraph "Region 2 - Standby"
        LB2[Load Balancer]
        APP2A[App Instance 2A]
        APP2B[App Instance 2B]
        REDIS2[Redis Replica]
        RDS2[RDS Read Replica]
    end
    
    subgraph "Global"
        DNS[Route53 / DNS]
        S3_GLOBAL[S3 Multi-Region<br/>AI Brain]
    end
    
    DNS -->|Primary| LB1
    DNS -.->|Failover| LB2
    
    LB1 --> APP1A
    LB1 --> APP1B
    LB2 --> APP2A
    LB2 --> APP2B
    
    APP1A --> REDIS1
    APP1B --> REDIS1
    APP2A --> REDIS2
    APP2B --> REDIS2
    
    REDIS1 -.->|Replication| REDIS2
    
    APP1A --> RDS1
    APP1B --> RDS1
    APP2A --> RDS2
    APP2B --> RDS2
    
    RDS1 -.->|Replication| RDS2
    
    APP1A --> S3_GLOBAL
    APP1B --> S3_GLOBAL
    APP2A --> S3_GLOBAL
    APP2B --> S3_GLOBAL
    
    style S3_GLOBAL fill:#e1f5ff
```

## Scaling Strategy

```mermaid
graph LR
    subgraph "Metrics"
        CPU[CPU Usage > 70%]
        MEM[Memory > 80%]
        REQ[Requests/sec > 100]
    end
    
    subgraph "Auto Scaling"
        TRIGGER[Scaling Trigger]
        SCALE_UP[Scale Up<br/>Add Instances]
        SCALE_DOWN[Scale Down<br/>Remove Instances]
    end
    
    subgraph "Constraints"
        MIN[Min: 2 instances]
        MAX[Max: 10 instances]
    end
    
    CPU --> TRIGGER
    MEM --> TRIGGER
    REQ --> TRIGGER
    
    TRIGGER -->|Threshold Exceeded| SCALE_UP
    TRIGGER -->|Below Threshold| SCALE_DOWN
    
    SCALE_UP -.->|Respect| MAX
    SCALE_DOWN -.->|Respect| MIN
```

## Monitoring Architecture

```mermaid
graph TB
    subgraph "Application Instances"
        APP1[App 1]
        APP2[App 2]
        APP3[App N]
    end
    
    subgraph "Metrics Collection"
        PROM[Prometheus]
        EXPORTER[Node Exporter]
    end
    
    subgraph "Visualization"
        GRAFANA[Grafana Dashboards]
    end
    
    subgraph "Alerting"
        ALERT[AlertManager]
        PAGERDUTY[PagerDuty]
        SLACK[Slack]
    end
    
    subgraph "Logging"
        LOGS[Application Logs]
        ELASTIC[Elasticsearch]
        KIBANA[Kibana]
    end
    
    APP1 -->|/metrics| PROM
    APP2 -->|/metrics| PROM
    APP3 -->|/metrics| PROM
    
    APP1 --> EXPORTER
    APP2 --> EXPORTER
    APP3 --> EXPORTER
    EXPORTER --> PROM
    
    PROM --> GRAFANA
    PROM --> ALERT
    
    ALERT --> PAGERDUTY
    ALERT --> SLACK
    
    APP1 --> LOGS
    APP2 --> LOGS
    APP3 --> LOGS
    LOGS --> ELASTIC
    ELASTIC --> KIBANA
```

## Security Architecture

```mermaid
graph TB
    subgraph "Edge Security"
        WAF[Web Application Firewall]
        DDOS[DDoS Protection]
    end
    
    subgraph "Network Security"
        VPC[Virtual Private Cloud]
        SG[Security Groups]
        NACL[Network ACLs]
    end
    
    subgraph "Application Security"
        AUTH[API Authentication]
        RATE[Rate Limiting]
        INPUT[Input Validation]
    end
    
    subgraph "Data Security"
        ENCRYPT_TRANSIT[TLS/SSL]
        ENCRYPT_REST[Encryption at Rest]
        SECRETS[Secrets Manager]
    end
    
    USERS[Users] --> WAF
    WAF --> DDOS
    DDOS --> VPC
    VPC --> SG
    SG --> NACL
    NACL --> AUTH
    AUTH --> RATE
    RATE --> INPUT
    
    INPUT --> ENCRYPT_TRANSIT
    ENCRYPT_TRANSIT --> ENCRYPT_REST
    ENCRYPT_REST --> SECRETS
```

## Deployment Environments

| Environment | Purpose | Instances | Database | Cache | LLM |
|-------------|---------|-----------|----------|-------|-----|
| **Development** | Local development | 1 | SQLite | In-memory | Mock |
| **Staging** | Pre-production testing | 2 | RDS (small) | Redis (small) | Real API (limited) |
| **Production** | Live system | 3-10 (auto-scale) | RDS (multi-AZ) | Redis (cluster) | Real API (full) |

## Infrastructure as Code

```yaml
# Example Kubernetes Deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pcf-builder
spec:
  replicas: 3
  selector:
    matchLabels:
      app: pcf-builder
  template:
    metadata:
      labels:
        app: pcf-builder
    spec:
      containers:
      - name: app
        image: pcf-builder:latest
        ports:
        - containerPort: 3000
        env:
        - name: NODE_ENV
          value: "production"
        volumeMounts:
        - name: ai-brain
          mountPath: /app/ai-brain
          readOnly: true
      volumes:
      - name: ai-brain
        persistentVolumeClaim:
          claimName: ai-brain-pvc
```

## Disaster Recovery

```mermaid
flowchart TD
    INCIDENT[Incident Detected]
    
    ASSESS{Severity}
    
    ASSESS -->|Low| AUTO_RECOVER[Auto Recovery<br/>Health Check Restart]
    ASSESS -->|Medium| MANUAL[Manual Investigation]
    ASSESS -->|High| FAILOVER[Initiate Failover]
    
    FAILOVER --> SWITCH_DNS[Switch DNS to Standby]
    SWITCH_DNS --> VERIFY[Verify Standby Health]
    VERIFY --> NOTIFY[Notify Stakeholders]
    
    MANUAL --> FIX[Apply Fix]
    FIX --> DEPLOY[Deploy Patch]
    
    AUTO_RECOVER --> MONITOR[Monitor]
    DEPLOY --> MONITOR
    NOTIFY --> MONITOR
    
    style INCIDENT fill:#ffcdd2
    style MONITOR fill:#c8e6c9
```
