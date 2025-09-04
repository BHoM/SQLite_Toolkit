# SQLite Adapter Architecture

This diagram shows how the SQLite_Adapter works within the BHoM framework, illustrating the data flow and component interactions.

```mermaid
flowchart TB
    Client[Client Application]
    Adapter[SQLiteAdapter]
    Push[Push Action]
    Pull[Pull Action]
    Execute[Execute Action]
    Create[Create Operations]
    Read[Read Operations]
    Update[Update Operations]
    Delete[Delete Operations]
    SQLiteDB[(SQLite Database)]
    
    %% Data Objects that can be pushed
    TableData[TableData]
    TableSchema[TableSchema]
    BHoMObjects[BHoM Objects]
    CustomObjects[Custom Objects]
    
    %% Request Objects for pulling
    CustomSqlRequest[CustomSqlRequest]
    SchemaRequest[SchemaRequest]
    TableRequest[TableRequest]
    TypeRequest[TypeRequest]
    
    %% Configuration Objects
    SQLiteSettings[SQLiteSettings]
    ActionConfig[ActionConfig]
    
    %% Main flow
    Client --> Adapter
    Adapter --> Push
    Adapter --> Pull
    Adapter --> Execute
    
    %% Push flow with data objects
    Push --> Create
    Push --> Update
    Create --> TableData
    Create --> TableSchema
    Create --> BHoMObjects
    Create --> CustomObjects
    Update --> TableData
    Update --> BHoMObjects
    TableData --> SQLiteDB
    TableSchema --> SQLiteDB
    BHoMObjects --> SQLiteDB
    CustomObjects --> SQLiteDB
    
    %% Pull flow with request objects
    Pull --> Read
    Read --> CustomSqlRequest
    Read --> SchemaRequest
    Read --> TableRequest
    Read --> TypeRequest
    CustomSqlRequest --> SQLiteDB
    SchemaRequest --> SQLiteDB
    TableRequest --> SQLiteDB
    TypeRequest --> SQLiteDB
    
    %% Execute flow
    Execute --> SQLiteDB
    
    %% Configuration
    SQLiteSettings --> Adapter
    ActionConfig --> Push
    ActionConfig --> Pull
    ActionConfig --> Execute
    
    %% Styling
    classDef adapterClass fill:#e1f5fe
    classDef actionClass fill:#f3e5f5
    classDef crudClass fill:#e8f5e8
    classDef dbClass fill:#e0f2f1
    classDef dataClass fill:#f1f8e9
    classDef requestClass fill:#fff3e0
    classDef configClass fill:#fce4ec
    
    class Adapter adapterClass
    class Push,Pull,Execute actionClass
    class Create,Read,Update,Delete crudClass
    class SQLiteDB dbClass
    class TableData,TableSchema,BHoMObjects,CustomObjects dataClass
    class CustomSqlRequest,SchemaRequest,TableRequest,TypeRequest requestClass
    class SQLiteSettings,ActionConfig configClass
```

## Key Components

### 1. **SQLiteAdapter (Main Class)**
- Inherits from `BHoMAdapter`
- Manages database connection lifecycle
- Handles SQLite-specific settings and configuration
- Maintains connection state and diagnostics

### 2. **Adapter Actions (Public Interface)**
- **Push**: Sends BHoM objects to SQLite database
- **Pull**: Retrieves data from SQLite database based on requests
- **Execute**: Executes commands like Open, Close, Custom operations

### 3. **CRUD Operations**
- **Create**: Creates tables, schemas, and inserts data
- **Read**: Handles various request types (Custom SQL, Schema, Table)
- **Update**: Modifies existing data
- **Delete**: Removes data from database

### 4. **Data Objects (Push Operations)**
- **TableData**: Contains table data and schema for insertion
- **TableSchema**: Database table structure definition
- **BHoM Objects**: Standard BHoM object types (Nodes, Elements, etc.)
- **Custom Objects**: User-defined custom objects

### 5. **Request Objects (Pull Operations)**
- **CustomSqlRequest**: Executes custom SQL queries
- **SchemaRequest**: Retrieves database schema information
- **TableRequest**: Fetches data from specific tables
- **TypeRequest**: Type-specific data retrieval

### 6. **Configuration Objects**
- **SQLiteSettings**: Database-specific configuration
- **ActionConfig**: Action-specific configuration

## Data Flow

1. **Push Flow**: Client → Push Action → CRUD Create/Update → Data Objects (TableData, TableSchema, BHoM Objects) → SQLite Database
2. **Pull Flow**: Client → Pull Action → CRUD Read → Request Objects → SQLite Database
3. **Execute Flow**: Client → Execute Action → Command Objects → SQLite Database

## Key Features

- **Connection Management**: Handles SQLite connection lifecycle
- **WAL Mode Support**: Write-Ahead Logging for better performance
- **Type Safety**: Strongly typed request and response objects
- **Error Handling**: Comprehensive error recording and handling
- **Configuration**: Flexible settings for different use cases
- **BHoM Integration**: Seamless integration with BHoM object model