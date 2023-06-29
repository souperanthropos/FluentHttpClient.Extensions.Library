## Middleware
- JwtBearerAuthManagerMiddleware

The middleware first examines the JWT token provided. It verifies whether the token is still valid or if it has expired. If the token is expired, it means that the user's session has ended, and they need to re-authenticate.
In order to handle this situation, the middleware triggers the `JwtBearerRefreshTokenProcessing` method. This method calls your implementation of refreshing the expired token.


Once the `JwtBearerRefreshTokenProcessing` method has successfully obtained a new token, it can update the request with the refreshed token and proceed with processing the request. This ensures that only requests with valid tokens are allowed to access protected resources on the server.


Add this middleware, the application can enforce token expiration checks before processing each request, ensuring that only authenticated and authorized users can access protected resources.
