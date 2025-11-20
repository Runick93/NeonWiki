# 📊 Diagramas Mermaid

Este archivo contiene ejemplos de diagramas Mermaid que puedes usar en tu wiki.

---

## 🔄 Diagrama de Flujo Simple

```mermaid
graph TD
    A[Inicio] --> B{¿Es válido?}
    B -->|Sí| C[Procesar]
    B -->|No| D[Mostrar Error]
    C --> E[Guardar]
    D --> F[Fin]
    E --> F
```

---

## 🎯 Diagrama de Secuencia

```mermaid
sequenceDiagram
    participant Usuario
    participant Aplicación
    participant BaseDatos
    
    Usuario->>Aplicación: Solicitar datos
    Aplicación->>BaseDatos: Consultar
    BaseDatos-->>Aplicación: Resultados
    Aplicación-->>Usuario: Mostrar datos
```

---

## 🏢 Diagrama de Estado

```mermaid
stateDiagram-v2
    [*] --> Inactivo
    Inactivo --> Activo: Activar
    Activo --> Procesando: Iniciar Proceso
    Procesando --> Activo: Completar
    Activo --> Inactivo: Desactivar
    Inactivo --> [*]
```

---

## 📈 Diagrama Gantt

```mermaid
gantt
    title Plan de Desarrollo
    dateFormat  YYYY-MM-DD
    section Fase 1
    Diseño           :a1, 2024-01-01, 30d
    section Fase 2
    Desarrollo       :a2, after a1, 45d
    section Fase 3
    Pruebas          :a3, after a2, 15d
```

---

## 🎨 Diagrama de Clases

```mermaid
classDiagram
    class MainWindow {
        -Dictionary webViews
        -Dictionary editors
        -string tempHtmlFolder
        +OpenFileInTab()
        +RenderHtmlForTab()
        +SaveFile()
    }
    
    class WebView2 {
        +CoreWebView2
        +Navigate()
    }
    
    MainWindow --> WebView2 : usa
```

---

## 🔀 Diagrama Entidad-Relación

```mermaid
erDiagram
    USUARIO ||--o{ ARCHIVO : crea
    USUARIO {
        string nombre
        string email
    }
    ARCHIVO {
        string nombre
        string contenido
        datetime fechaCreacion
    }
```

---

## 📋 Pie Chart

```mermaid
pie title Distribución de Tareas
    "Completadas" : 45
    "En Progreso" : 25
    "Pendientes" : 30
```

---

## 🎓 Diagrama de Clase UML Completo

```mermaid
classDiagram
    class NeonWiki {
        -string currentWikiPath
        -Dictionary tabItems
        +LoadWikiStructure()
        +OpenFileInTab()
    }
    
    class TabItem {
        +string header
        +Content content
    }
    
    class WebView2 {
        +CoreWebView2 core
        +Navigate(url)
    }
    
    class Editor {
        +string text
        +void Save()
    }
    
    NeonWiki --> TabItem : contiene
    TabItem --> WebView2 : vista
    TabItem --> Editor : editor
```

---

## 💡 Tipos de Diagramas Soportados

Mermaid soporta muchos tipos de diagramas:

- **Flowchart** (graph TD, graph LR)
- **Sequence Diagram** (sequenceDiagram)
- **State Diagram** (stateDiagram)
- **Gantt Chart** (gantt)
- **Class Diagram** (classDiagram)
- **Entity Relationship** (erDiagram)
- **Pie Chart** (pie)
- **Journey Diagram** (journey)
- **Git Graph** (gitGraph)
- Y más...

---

## 📝 Cómo Usar

1. Escribe tu código Mermaid en un bloque de código markdown
2. Especifica el lenguaje como `mermaid`:
   ````markdown
   ```mermaid
   graph TD
       A --> B
   ```
   ````
3. El diagrama se renderizará automáticamente con el tema NEON 🎨

---

**¡Disfruta creando diagramas en tu wiki!** ⚡

