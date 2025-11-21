# Guía Completa de Diagramas Mermaid

## Índice

1. [Flowchart (Diagrama de Flujo)](#1-flowchart-diagrama-de-flujo)
2. [Sequence Diagram (Diagrama de Secuencia)](#2-sequence-diagram-diagrama-de-secuencia)
3. [Class Diagram (Diagrama de Clases)](#3-class-diagram-diagrama-de-clases)
4. [State Diagram (Diagrama de Estados)](#4-state-diagram-diagrama-de-estados)
5. [Entity Relationship Diagram (Diagrama ER)](#5-entity-relationship-diagram-diagrama-er)
6. [User Journey (Viaje del Usuario)](#6-user-journey-viaje-del-usuario)
7. [Gantt Diagram (Diagrama de Gantt)](#7-gantt-diagram-diagrama-de-gantt)
8. [Pie Chart (Gráfico Circular)](#8-pie-chart-gráfico-circular)
9. [Quadrant Chart (Gráfico de Cuadrantes)](#9-quadrant-chart-gráfico-de-cuadrantes)
10. [Requirement Diagram (Diagrama de Requisitos)](#10-requirement-diagram-diagrama-de-requisitos)
11. [Gitgraph (Gráfico Git)](#11-gitgraph-gráfico-git)
12. [C4 Diagram (Diagrama C4)](#12-c4-diagram-diagrama-c4)
13. [Mindmap (Mapa Mental)](#13-mindmap-mapa-mental)
14. [Timeline (Línea de Tiempo)](#14-timeline-línea-de-tiempo)
15. [Sankey Diagram (Diagrama Sankey)](#15-sankey-diagram-diagrama-sankey)
16. [XY Chart (Gráfico XY)](#16-xy-chart-gráfico-xy)
17. [Block Diagram (Diagrama de Bloques)](#17-block-diagram-diagrama-de-bloques)

---

## 1. Flowchart (Diagrama de Flujo)

Ideal para representar procesos, algoritmos y flujos de trabajo.

```mermaid
flowchart TD
    A[Inicio] --> B{¿Tiene acceso?}
    B -->|Sí| C[Procesar solicitud]
    B -->|No| D[Denegar acceso]
    C --> E[Validar datos]
    E --> F{¿Datos válidos?}
    F -->|Sí| G[Guardar en DB]
    F -->|No| H[Mostrar error]
    G --> I[Fin]
    H --> I
    D --> I
```

**Direcciones disponibles:**
- `TD` o `TB` (Top Down)
- `BT` (Bottom to Top)
- `LR` (Left to Right)
- `RL` (Right to Left)

---

## 2. Sequence Diagram (Diagrama de Secuencia)

Perfecto para mostrar interacciones entre objetos o componentes en el tiempo.

```mermaid
sequenceDiagram
    participant Cliente
    participant API
    participant DB
    participant MQ as IBM MQ
    
    Cliente->>+API: POST /message
    API->>+DB: Validar usuario
    DB-->>-API: Usuario válido
    API->>+MQ: Enviar mensaje
    MQ-->>-API: ACK
    API-->>-Cliente: 200 OK
    
    Note over API,MQ: Conexión pooling activa
    
    alt Error en MQ
        MQ--xAPI: Error conexión
        API->>API: Reintentar con backup
    end
```

---

## 3. Class Diagram (Diagrama de Clases)

Para modelar estructuras orientadas a objetos, clases y sus relaciones.

```mermaid
classDiagram
    class ConnectionPool {
        -List~Connection~ connections
        -int maxSize
        -int minSize
        +GetConnection() Connection
        +ReleaseConnection(Connection)
        +HealthCheck() bool
    }
    
    class Connection {
        -string connectionId
        -DateTime lastUsed
        -bool isHealthy
        +Open()
        +Close()
        +Send(Message)
    }
    
    class LoadBalancer {
        -string strategy
        +SelectConnection() Connection
        +UpdateMetrics()
    }
    
    class HealthMonitor {
        -Timer timer
        +CheckHealth()
        +ReportMetrics()
    }
    
    ConnectionPool "1" --> "*" Connection : manages
    ConnectionPool "1" --> "1" LoadBalancer : uses
    ConnectionPool "1" --> "1" HealthMonitor : monitors
```

---

## 4. State Diagram (Diagrama de Estados)

Representa los estados de un sistema y las transiciones entre ellos.

```mermaid
stateDiagram-v2
    [*] --> Idle
    
    Idle --> Connecting : Connect()
    Connecting --> Active : Success
    Connecting --> Failed : Error
    
    Active --> Busy : Send()
    Busy --> Active : Complete
    Active --> Idle : Disconnect()
    
    Failed --> Connecting : Retry
    Failed --> [*] : Max retries
    
    Active --> Unhealthy : Health check failed
    Unhealthy --> Active : Recovered
    Unhealthy --> Failed : Critical
```

---

## 5. Entity Relationship Diagram (Diagrama ER)

Para modelar bases de datos y relaciones entre entidades.

```mermaid
erDiagram
    CONNECTION ||--o{ MESSAGE : sends
    CONNECTION {
        string connectionId PK
        string queueManager
        datetime createdAt
        int messageCount
    }
    
    MESSAGE {
        string messageId PK
        string connectionId FK
        string payload
        datetime timestamp
        string status
    }
    
    POOL ||--|{ CONNECTION : contains
    POOL {
        string poolId PK
        int maxSize
        int currentSize
        string strategy
    }
    
    METRICS }o--|| CONNECTION : tracks
    METRICS {
        string metricId PK
        string connectionId FK
        float throughput
        int errors
        datetime recorded
    }
```

---

## 6. User Journey (Viaje del Usuario)

Documenta la experiencia del usuario a través de diferentes etapas.

```mermaid
journey
    title Desarrollador usando ASSIGN 2.0
    section Configuración
      Instalar servicio: 5: Dev
      Configurar pools: 4: Dev
      Definir QMs: 4: Dev
    section Operación
      Enviar mensaje: 5: Dev, Ops
      Monitorear métricas: 5: Ops
      Revisar logs: 3: Ops
    section Troubleshooting
      Detectar error: 2: Ops
      Analizar logs: 3: Dev, Ops
      Aplicar fix: 4: Dev
      Verificar solución: 5: Ops
```

---

## 7. Gantt Diagram (Diagrama de Gantt)

Para planificación de proyectos y cronogramas.

```mermaid
gantt
    title Desarrollo ASSIGN 2.0
    dateFormat YYYY-MM-DD
    section Fase 1
    Diseño arquitectura     :done, arch, 2024-01-01, 15d
    Pool Manager           :done, pool, after arch, 20d
    Load Balancer          :done, lb, after pool, 15d
    
    section Fase 2
    Health Monitor         :active, health, after lb, 10d
    Circuit Breaker        :crit, cb, after health, 12d
    Metrics & Logging      :metrics, after cb, 10d
    
    section Fase 3
    Testing integración    :test, after metrics, 15d
    Documentación         :docs, after test, 10d
    Deployment            :deploy, after docs, 5d
```

---

## 8. Pie Chart (Gráfico Circular)

Para mostrar proporciones y distribuciones.

```mermaid
pie title Distribución de Mensajes por Queue Manager
    "QM_PROD_01" : 35
    "QM_PROD_02" : 28
    "QM_PROD_03" : 22
    "QM_BACKUP_01" : 10
    "QM_BACKUP_02" : 5
```

---

## 9. Quadrant Chart (Gráfico de Cuadrantes)

Para análisis de prioridades o clasificación en dos dimensiones.

```mermaid
quadrantChart
    title Priorización de Features
    x-axis Bajo Esfuerzo --> Alto Esfuerzo
    y-axis Bajo Impacto --> Alto Impacto
    quadrant-1 Implementar después
    quadrant-2 Implementar primero
    quadrant-3 No prioritario
    quadrant-4 Quick wins
    
    Circuit Breaker: [0.7, 0.9]
    Health Monitor: [0.5, 0.85]
    Metrics Dashboard: [0.6, 0.7]
    Connection Warmup: [0.3, 0.8]
    Auto-scaling: [0.8, 0.6]
    Retry Logic: [0.2, 0.9]
    Logging: [0.3, 0.5]
    Documentation: [0.4, 0.4]
```

---

## 10. Requirement Diagram (Diagrama de Requisitos)

Para modelar requisitos y sus relaciones.

```mermaid
requirementDiagram
    requirement high_availability {
        id: REQ-001
        text: Sistema debe tener 99.9% uptime
        risk: high
        verifymethod: test
    }
    
    requirement performance {
        id: REQ-002
        text: Procesar 1200 msg/seg
        risk: medium
        verifymethod: test
    }
    
    requirement monitoring {
        id: REQ-003
        text: Métricas en tiempo real
        risk: low
        verifymethod: inspection
    }
    
    functionalRequirement connection_pool {
        id: FR-001
        text: Pool de conexiones configurable
        risk: medium
        verifymethod: test
    }
    
    performanceRequirement latency {
        id: PR-001
        text: Latencia < 50ms
        risk: high
        verifymethod: test
    }
    
    high_availability - satisfies -> connection_pool
    performance - satisfies -> connection_pool
    monitoring - satisfies -> connection_pool
    connection_pool - refines -> latency
```

---

## 11. Gitgraph (Gráfico Git)

Para visualizar ramas y estrategias de versionamiento.

```mermaid
gitGraph
    commit id: "Initial commit"
    commit id: "Setup project"
    
    branch develop
    checkout develop
    commit id: "Add Pool Manager"
    commit id: "Add Load Balancer"
    
    branch feature/health-monitor
    checkout feature/health-monitor
    commit id: "Implement health checks"
    commit id: "Add metrics"
    
    checkout develop
    merge feature/health-monitor
    
    branch feature/circuit-breaker
    checkout feature/circuit-breaker
    commit id: "Implement circuit breaker"
    
    checkout develop
    commit id: "Update config"
    merge feature/circuit-breaker
    
    checkout main
    merge develop tag: "v2.0.0"
```

---

## 12. C4 Diagram (Diagrama C4)

Para arquitectura de software en diferentes niveles de abstracción.

```mermaid
C4Context
    title Contexto del Sistema ASSIGN 2.0
    
    Person(dev, "Desarrollador", "Consume el servicio")
    Person(ops, "Operador", "Monitorea el sistema")
    
    System(assign, "ASSIGN 2.0", "Sistema de pool de conexiones IBM MQ")
    
    System_Ext(mq, "IBM MQ", "Sistema de mensajería mainframe")
    System_Ext(prometheus, "Prometheus", "Monitoreo y métricas")
    System_Ext(elk, "ELK Stack", "Logs centralizados")
    
    Rel(dev, assign, "Envía mensajes", "WCF/SOAP")
    Rel(ops, prometheus, "Consulta métricas", "HTTPS")
    Rel(assign, mq, "Envía/Recibe mensajes", "IBM MQ Client")
    Rel(assign, prometheus, "Exporta métricas", "HTTP")
    Rel(assign, elk, "Envía logs", "Serilog")
```

---

## 13. Mindmap (Mapa Mental)

Para brainstorming y organización de ideas.

```mermaid
mindmap
  root((ASSIGN 2.0))
    Arquitectura
      Clean Architecture
      5 Capas
        WcfService
        MqConnector
        PoolManager
        LoadBalancer
        QueueHandler
      Patrones
        Circuit Breaker
        Health Monitor
        Connection Pool
    Performance
      7M msg/día
      1200 msg/seg
      20 servidores
      Latencia bajo 50ms
    Observabilidad
      Serilog
      Prometheus
      Métricas
        Throughput
        Latencia
        Error rate
    Resiliencia
      Failover
      Retry logic
      Connection warmup
      Auto-recovery
```

---

## 14. Timeline (Línea de Tiempo)

Para mostrar eventos cronológicos.

```mermaid
timeline
    title Historia de Desarrollo ASSIGN 2.0
    
    2024-01 : Análisis inicial
           : Diseño arquitectura
    
    2024-02 : Desarrollo Pool Manager
           : Implementación Load Balancer
    
    2024-03 : Health Monitor
           : Circuit Breaker
           : Primera versión funcional
    
    2024-04 : Testing intensivo
           : Optimización performance
    
    2024-05 : Métricas y observabilidad
           : Documentación técnica
    
    2024-06 : Deployment producción
           : Monitoreo 24/7
```

---

## 15. Sankey Diagram (Diagrama Sankey)

Para visualizar flujos y distribuciones.

```mermaid
sankey-beta

%% Flujo de mensajes a través del sistema
Aplicaciones,Pool Manager,1000
Pool Manager,QM_PROD_01,350
Pool Manager,QM_PROD_02,280
Pool Manager,QM_PROD_03,220
Pool Manager,QM_BACKUP,150

QM_PROD_01,Procesados,330
QM_PROD_01,Errores,20

QM_PROD_02,Procesados,270
QM_PROD_02,Errores,10

QM_PROD_03,Procesados,215
QM_PROD_03,Errores,5

QM_BACKUP,Procesados,145
QM_BACKUP,Errores,5
```

---

## 16. XY Chart (Gráfico XY)

Para visualizar datos en ejes cartesianos.

```mermaid
xychart-beta
    title "Throughput por Hora - ASSIGN 2.0"
    x-axis [00:00, 04:00, 08:00, 12:00, 16:00, 20:00, 23:59]
    y-axis "Mensajes/segundo" 0 --> 1400
    line [200, 150, 800, 1200, 1100, 900, 400]
    bar [180, 140, 750, 1150, 1050, 850, 380]
```

---

## 17. Block Diagram (Diagrama de Bloques)

Para representar componentes y sus conexiones (experimental).

```mermaid
block-beta
    columns 3
    
    space:1 Cliente space:1
    space:1 down<["WCF"]>(down):1 space:1
    
    WcfService
    down<["Pool Manager"]>(down)
    space:1
    
    LoadBalancer
    HealthMonitor
    Metrics
    
    space:1
    down<["Connections"]>(down)
    space:1
    
    QM1["QM_PROD_01"]
    QM2["QM_PROD_02"]
    QM3["QM_PROD_03"]
    
    Cliente --> WcfService
    WcfService --> LoadBalancer
    LoadBalancer --> QM1
    LoadBalancer --> QM2
    LoadBalancer --> QM3
    HealthMonitor --> QM1
    HealthMonitor --> QM2
    HealthMonitor --> QM3
```

---

## Notas Adicionales

### Sintaxis Básica

- Los diagramas siempre inician con el tipo: `flowchart`, `sequenceDiagram`, `classDiagram`, etc.
- Los comentarios se hacen con `%%`
- Los estilos se pueden aplicar con `style` o `classDef`

### Compatibilidad

Estos diagramas funcionan en:
- GitHub/GitLab markdown
- Documentación técnica
- Notion, Obsidian
- VS Code con extensiones
- Tu aplicación Mini-Wiki Markdown 😉

### Recursos

- [Documentación oficial Mermaid](https://mermaid.js.org/)
- [Live Editor](https://mermaid.live/)
- [Mermaid Chart](https://www.mermaidchart.com/)

---

**Creado para navegación rápida y referencia técnica** 🚀
