## 4. DTOs

**Principle:** a DTO is the shape of data crossing a boundary. Entities are the shape of data *inside* the domain. Keeping them separate means you can change the database model without breaking your API, and you never accidentally leak a `PasswordHash` or an internal navigation property to a caller.

**Rules for every DTO:**
- Declare them as `record` types (immutable, value-based — the right tool for a data contract).
- Property names in `camelCase` when serialized; UTC timestamps carry a `Utc` suffix in their name.
- **Never** reference a Domain entity or enum-heavy entity graph from a DTO. If a DTO needs a status, expose it as a simple string or the enum, but never embed the whole entity.
- Separate **input** DTOs (what a caller sends) from **output** DTOs (what a caller receives). Do not reuse one record for both — they evolve differently and have different validation needs.

**Steps:**
1. Create a **generic paged-result DTO** to carry list responses. It must hold the items plus `page`, `pageSize`, and `totalCount` (this matches the pagination envelope in the frozen spec). Every list endpoint returns this shape.
2. Create the **Auth** DTOs: a registration input (name, email, password, role), a login input (email, password), and an auth-result output (the token string and its `expiresAtUtc`). 
2.1 The result must **not** carry the password hash or any entity. 

3.1. Create the **Event** DTOs: a create-event input (title, description, venue name, start time), and an event output that exposes only what a client should see (id, title, description, venue, start time, status, organizer id).
3.2. Decide deliberately whether the output includes its ticket types or not, and be able to justify it.

4. Create the **TicketType** DTOs: an add-ticket-type input (name, price, total quantity), and a ticket-type output (id, event id, name, price, total quantity, **available quantity**). Note that `RowVersion` is an internal concurrency concern and must **not** appear in any DTO.
5. Create the **Reservation** DTOs: a create-hold input (quantity), and a reservation output (id, ticket type id, quantity, status, `holdExpiresAtUtc`, `createdAtUtc`, and `confirmedAtUtc` when set).

**Think about:** which fields are server-controlled and must never be accepted from the client (ids, timestamps, status, available quantity, the organizer id on an event). 
If an input DTO contains one of those, that is a bug — the client does not get to set it.
