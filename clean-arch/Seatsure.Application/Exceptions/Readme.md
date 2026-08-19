
## 5. Exception types

**Principle:** the service layer signals *what went wrong in business terms* — "that event doesn't exist", "that email is taken", "someone booked the last ticket first". It must not know or care that those become HTTP 404 / 409 / etc. — that translation is the controller's job in a later task. Typed exceptions are how the service communicates the *category* of failure without depending on the web layer.

**Tradeoff to understand and be ready to defend:** exceptions vs. a result/outcome object (e.g., a discriminated result). Exceptions keep the happy-path code clean and are idiomatic in ASP.NET, but they use control flow for expected conditions and cost more when thrown frequently. For this project we use exceptions — know *why* you'd reconsider that if a failure became a common, expected outcome rather than an exceptional one.


**Steps — create one exception type per failure category.** Each should carry a clear message and, where useful, the identifier that was not found or that conflicted:

1. A **not-found** exception — an entity was requested by id and does not exist. (→ maps to 404 later.)
2. A **validation** exception — input broke a business rule the DTO shape alone can't express (e.g., quantity < 1, price ≤ 0, page < 1). (→ 400.)
3. A **conflict** exception — the request collided with the current state. This one is used in **two distinct situations**, so make sure its message can express both: (a) insufficient inventory / a concurrency conflict on a reservation hold, and (b) an illegal state transition such as confirming an already-expired or already-confirmed reservation. (→ 409.)
4. An **email-taken / duplicate** exception for registration when the email already exists. You may model this as its own type or as a specific conflict — decide and justify. (→ 409.)
5. An **authorization/ownership** exception — the caller is authenticated but not allowed to act on this resource (e.g., publishing an event they don't own). (→ 403.)

**Guidance:** give the exceptions a small, shared base if it helps you group them, but don't over-engineer a deep hierarchy. The goal is that a later middleware can pattern-match the exception type to a status code in one place.
