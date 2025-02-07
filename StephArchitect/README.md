TODO: update this

# ProjectName.sln

- **ProjectName.Domain**
    - **Users/**
        - **User**

- **ProjectName.Contracts**
    - **Users/**
        - **Requests**
        - **Responses**
        - **Interfaces**
      
- **ProjectName.Application** (References Domain & Contracts)
    - **Users/**
      - **Commands/**
        - **CreateUserCommand**
      - **Queries/**
        - **GetUserByIdQuery**
    - DependencyInjection.cs

- **ProjectName.Infrastructure** (References Application)
    - DependencyInjection.cs

- **ProjectName.Persistence** (References Application)
    - **Users/**
        - UserRepository
        - UserEntityConfiguration.cs
    - **Migrations/**
    - ProjectNameDbContext.cs
    - DependencyInjection.cs

- **ProjectName.Api** (References Application, & Infrastructure)
    - **Users/**
        - UserEndpoints
    - Program.cs
    - appsettings.json
    - appsettings.Development.json


// Keep: