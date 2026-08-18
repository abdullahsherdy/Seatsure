|_IPasswordHasher -> Interface for password hashing and verification. (hash, verify)
|_ BcryptPasswordHasher -> Implementation of IPasswordHasher using BCrypt for secure password hashing and verification.
|_ jwtOptions -> Configuration options for JWT token generation, including secret key, issuer, audience, and expiration settings.(skeleton)
|_ IJWtService -> Interface for JWT token generation and validation. (generate) 
|_ JwtService -> Implementation of IJWtService using JWT for token generation and validation. taks jwtoptions