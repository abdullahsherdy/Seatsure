
## DbContext with Repository Pattern 
- mo need for IRepository, just use the repository name as the interface name, e.g. IUserRepository -> UserRepository
- no IRepsository, because DbContext is already an abstraction, and the repository is just a wrapper around it.
- also DbContext is a unit of work, so no need for a separate unit of work interface. 

>so the best practice for repository pattern is to have a repository interface for each entity, and a repository implementation for each entity, 
>and the repository implementation should use the DbContext to perform CRUD operations on the entity. 


## Clean Architecture 
>the best or common Clean Architecture for a repository pattern is to have a repository interface for each entity, and a repository implementation for each entity,
>and the repository implementation should use the DbContext to perform CRUD operations on the entity.
>Clean Architecture is a software design pattern that separates the concerns of the application into different layers, and the repository pattern is a way to abstract the data access layer from the rest of the application.
>Clean Architecture folder structor 

>in Clean Architecture, business logic is in the Application Layer, and the data access logic is in the Infrastructure Layer. 
> The Application Layer defines the repository interfaces, and the Infrastructure Layer implements those interfaces using Entity Framework Core and DbContext. 
>so the business logic is in the Application Layer, and the data access logic is in the Infrastructure Layer.

## why build a repository pattern?
1. to abstract the data access layer from the rest of the application, so that the application can be easily tested and maintained.
2. to provide a consistent API for the application to access the data, so that

## why business logic is in the Application Layer, and the data access logic is in the Infrastructure Layer?
1. because the Application Layer is where the business logic resides, and the Infrastructure Layer is where the data access logic resides.
2. The Application Layer defines the repository interfaces, and the Infrastructure Layer implements those interfaces using Entity Framework Core and DbContext.
3. This separation of concerns allows for easier testing and maintenance of the application, as well as a consistent API for accessing the data.


> The Domain Layer contains the domain entities and domain services that use those entities. 
The API Layer contains the controllers and API endpoints that use the application services.


```
    |____ API => contains the controllers, and the API endpoints that use the application services
    |____ Infrastructure -> contains the implementation of the repository interfaces, and the implementation of the DbContext
    |____ Application -> contains the repository interfaces, and the application services that use the repository interfaces
    |
    |___ Domain -> contains the domain entities, and the domain services that use the domain entities

```

## the next session, plan?
1. explain the repository pattern, real benefits of it, and when to use and when not to use. 
2. Dbcontext vs repository pattern 
3. how to determine the exitance of business logic?
4. live coding -> repsitory pattern and BL but following clean architecture common folder structure. 
5. Q&A 
