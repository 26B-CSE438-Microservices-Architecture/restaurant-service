## Restaurant Service - Trendyol GO Clone
The Restaurant Service is the authoritative source of truth for all restaurant-related data and discovery within the microservices ecosystem. Since there is no standalone search service, this component also manages the Search & Filtering logic for the entire platform.

## Key Features
Restaurant Management: Onboarding new partners and managing profile metadata (location, contact, etc.).

Search & Discovery: Handling keyword searches (e.g., "Burger") and mutfak (cuisine) filtering directly from the database.

Dynamic Menus: Managing hierarchical structures for categories and products.

Real-time Availability: Toggling store status (Open/Closed/Busy) and product stock levels.

Geospatial Support: Storing coordinates for distance-based sorting (finding restaurants near the user).

Operational Rules: Managing minimum order amounts and delivery fees per restaurant.

## System Requirements
1. Functional Requirements
Onboarding: Allow restaurant partners to register with physical locations (Lat/Long) and operating hours.

Search Engine: Provide a search interface to find restaurants by name, category, or food items.

Menu Management: Create/Update/Delete menu categories and individual products.

Emergency Toggle: Allow owners to close the restaurant immediately during high-occupancy periods.

Stock Management: Instant "Out of Stock" marking for products.

Location Filtering: List restaurants based on the user's current coordinates.

2. Non-Functional Requirements
High Availability: As the primary catalog, the service must be available for users to browse even if payment or order services are under maintenance.

Read Performance: Since it handles all search queries, read operations must be optimized (using Indexes or Caching) to handle high traffic.

Scalability: Must support concurrent users browsing menus simultaneously.

## Interfaces & Communication
1. REST API (Primary Interface)
Used by the API Gateway to serve both the Mobile and Frontend applications.

Discovery Endpoints: Used by customers to find where to eat.

Management Endpoints: Used by the User/Owner to update their store.

2. gRPC (Synchronous Internal)
Critical for high-speed data validation between services.

Price & Stock Validation: A gRPC call from the Order Service to the Restaurant Service during checkout to ensure the item is still available and the price is correct.

3. Event-Driven Interface (Async - Pub/Sub)
Broadcasts changes to the rest of the system via RabbitMQ/Kafka:

Topic: restaurant-status-events -> Notifies the system of store closures so the Mobile app can update the UI.

Topic: stock-alerts -> Notifies the Order Service to prevent customers from adding unavailable items to their cart.

## Database Schema
1. Restaurant Entity
id: UUID (Primary Key)

name: String (Brand name)

description: String (Cuisine info)

address_text: String (Physical address)

latitude / longitude: Decimal (For proximity-based searching)

logo_url: String (Image URL)

min_order_amount: Decimal

delivery_fee: Decimal

is_active: Boolean (Master status)

opening_time / closing_time: Time

2. MenuCategory Entity
id: UUID (Primary Key)

restaurant_id: UUID (Foreign Key)

name: String (e.g., "Burgers", "Drinks")

display_order: Integer (Sort order)

3. Product Entity
id: UUID (Primary Key)

category_id: UUID (Foreign Key)

name: String (e.g., "Classic Burger")

price: Decimal

is_available: Boolean (Stock flag)

image_url: String

## API Endpoints
Customer & Search Scope
GET /api/v1/restaurants - List all restaurants. (Filters: ?name=..., ?cuisine=..., ?lat=...&long=...).

GET /api/v1/restaurants/{id}/menu - Fetch categories and nested products for a store.

GET /api/v1/restaurants/top-rated - Get featured restaurants.

Management Scope
POST /api/v1/restaurants - Register new restaurant.

PATCH /api/v1/restaurants/{id}/status - Toggle Open/Busy/Closed.

PATCH /api/v1/products/{id}/stock - Update product availability.

PUT /api/v1/products/{id} - Update price or description.

## Inter-Service Communication Logic
Direct Search Responsibility: Because there is no separate Search Service, this service must implement efficient database indexing (e.g., B-Tree on name, GIST for coordinates) to ensure search results are fast.

Order Validation: When the Order Service receives a request, it calls this service via gRPC to confirm the basket items are valid before proceeding to the Payment Service.

UI Updates: Upon a status change (e.g., Restaurant Closes), an event is fired. The Mobile/Frontend apps listen for this to gray out the restaurant in the UI.