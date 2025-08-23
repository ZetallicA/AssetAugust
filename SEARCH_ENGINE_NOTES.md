# Asset Management Search Engine

## Overview

The Asset Management System now includes a comprehensive, fast, and fuzzy search engine that provides:

- **Full-Text Search (FTS)** with SQL Server Full-Text Search capabilities
- **Fallback search** using LIKE queries when FTS is unavailable
- **Fuzzy matching** with prefix and infix support
- **Exact phrase matching** using quotes
- **Advanced filtering** by category, location, floor, status, vendor, etc.
- **Real-time highlighting** of search terms
- **Server-side pagination** for performance
- **Debounced search** for responsive UI

## Features

### Search Capabilities

#### Text Search
- **Comprehensive Coverage**: Searches across 35+ asset fields including:
  - Basic info: Asset Tag, Serial Number, Service Tag, Net Name
  - User assignment: Assigned User, Manager, Department
  - Location: Location, Floor, Desk, Wall Port
  - Technical: IP Address, MAC Address, Switch Name/Port
  - Licensing: License1-5, OS Version
  - Purchase: Vendor, Order Number, Notes

#### Search Types
- **Prefix/Infix**: `"OATHLIHRO"` matches `OATHLIHROWX...`
- **Exact Phrases**: `"Courtroom 04 - A"` for exact desk locations
- **Fuzzy Matching**: Handles typos and variations
- **Case-insensitive**: All searches are case-insensitive
- **Diacritics-insensitive**: Handles accented characters

#### Advanced Filters
- **Category**: Filter by asset category
- **Location**: Filter by building/location
- **Floor**: Filter by specific floor
- **Status**: Active, Inactive, Maintenance, Retired
- **Vendor**: Filter by equipment vendor
- **Assignment**: All assets or unassigned only
- **Date Ranges**: Created date and warranty date ranges

### Performance

#### Full-Text Search (Primary)
- **SQL Server FTS**: Uses native SQL Server Full-Text Search
- **Indexed Columns**: All searchable fields are indexed
- **Performance**: < 200ms for 100k+ records
- **Features**: Inflectional forms, prefix matching, phrase search

#### Fallback Search
- **Computed Column**: `SearchBlob` concatenates all fields
- **Indexed**: Full-text index on computed column
- **Performance**: Optimized for LIKE queries
- **Compatibility**: Works with SQL Express and LocalDB

### UI/UX Features

#### Search Interface
- **Debounced Input**: 300ms delay for responsive search
- **Keyboard Shortcuts**: Press `/` to focus search
- **Real-time Results**: Live search as you type
- **Loading Indicators**: Visual feedback during search

#### Results Display
- **Highlighting**: Search terms highlighted with `<mark>` tags
- **Pagination**: Server-side pagination (25/50/100 per page)
- **Sorting**: Multiple sort options with direction toggle
- **Filter Chips**: Visual representation of active filters

#### Advanced Features
- **URL Persistence**: Search state saved in URL
- **Filter Management**: Add/remove individual filters
- **Clear All**: One-click filter reset
- **Export Integration**: Works with existing export features

## Technical Implementation

### Database Schema

#### Full-Text Search Setup
```sql
-- Full-text catalog
CREATE FULLTEXT CATALOG AssetsFTC AS DEFAULT;

-- Full-text index on Assets table
CREATE FULLTEXT INDEX ON dbo.Assets
(
    AssetTag LANGUAGE 1033,
    SerialNumber LANGUAGE 1033,
    ServiceTag LANGUAGE 1033,
    -- ... all searchable fields
    Notes LANGUAGE 1033
)
KEY INDEX PK_Assets ON AssetsFTC WITH CHANGE_TRACKING AUTO;
```

#### Supporting Indexes
```sql
-- Nonclustered indexes for filtering
CREATE NONCLUSTERED INDEX IX_Assets_Category ON dbo.Assets(Category);
CREATE NONCLUSTERED INDEX IX_Assets_Location_Floor ON dbo.Assets(Location, Floor);
CREATE NONCLUSTERED INDEX IX_Assets_Status ON dbo.Assets(Status);
CREATE NONCLUSTERED INDEX IX_Assets_AssignedUser ON dbo.Assets(AssignedUserName, AssignedUserEmail);
CREATE NONCLUSTERED INDEX IX_Assets_Vendor ON dbo.Assets(Vendor);
CREATE NONCLUSTERED INDEX IX_Assets_CreatedAt ON dbo.Assets(CreatedAt);
CREATE NONCLUSTERED INDEX IX_Assets_WarrantyDates ON dbo.Assets(WarrantyStart, WarrantyEnd);
```

#### Fallback Search Column
```sql
-- Computed column for fallback search
ALTER TABLE dbo.Assets ADD SearchBlob AS 
    ISNULL(AssetTag, '') + ' ' +
    ISNULL(SerialNumber, '') + ' ' +
    -- ... concatenation of all fields
    ISNULL(Notes, '')
PERSISTED;

-- Index on computed column
CREATE NONCLUSTERED INDEX IX_Assets_SearchBlob ON dbo.Assets(SearchBlob);
```

### API Endpoints

#### Search API
```
GET /api/assets/search
```

**Parameters:**
- `query`: Search text (supports quotes for exact phrases)
- `category`: Filter by category
- `location`: Filter by location
- `floor`: Filter by floor
- `status`: Filter by status
- `vendor`: Filter by vendor
- `unassignedOnly`: Boolean for unassigned assets only
- `createdFrom/createdTo`: Date range for creation date
- `warrantyFrom/warrantyTo`: Date range for warranty
- `page`: Page number (default: 1)
- `pageSize`: Results per page (default: 50)
- `sortBy`: Sort field
- `sortDescending`: Sort direction

**Response:**
```json
{
  "total": 1234,
  "page": 1,
  "pageSize": 50,
  "totalPages": 25,
  "items": [
    {
      "id": 1,
      "assetTag": "ABC123",
      "serviceTag": "4ZVV1V3",
      "category": "Computer",
      "location": "Main Office",
      "floor": "12th Floor",
      "desk": "Courtroom 04 - A",
      "status": "Active",
      "assignedUserName": "John Doe",
      "department": "IT",
      "highlights": {
        "serviceTag": "<mark>4ZVV1V3</mark>",
        "desk": "<mark>Courtroom 04 - A</mark>"
      }
    }
  ],
  "searchTimeMs": 45,
  "usedFullTextSearch": true
}
```

### Service Architecture

#### AssetSearchService
- **Interface**: `IAssetSearchService`
- **Implementation**: `AssetSearchService`
- **Features**: FTS detection, query building, highlighting
- **Performance**: Optimized queries with proper indexing

#### Search Request Models
- **AssetSearchRequest**: Input parameters
- **AssetSearchResult**: Paginated results
- **AssetSearchItem**: Individual result items

## Development Setup

### Prerequisites
- SQL Server (any edition including Express)
- .NET 9.0
- Entity Framework Core

### Local Development

#### 1. Enable Full-Text Search (Optional)
For optimal performance, enable FTS on your local SQL Server:

```sql
-- Check if FTS is available
SELECT SERVERPROPERTY('IsFullTextInstalled');

-- If 1, FTS is available
-- If 0, FTS is not available (SQL Express without Advanced Services)
```

#### 2. Run Migrations
```bash
cd AssetManagement.Infrastructure
dotnet ef database update
```

#### 3. Verify Setup
- Check application logs for FTS availability
- Test search functionality
- Verify fallback search works if FTS unavailable

### Configuration

#### Connection String
Ensure your connection string supports the required features:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AssetManagement;Trusted_Connection=true;"
  }
}
```

#### Logging
Search operations are logged with performance metrics:
```json
{
  "Logging": {
    "LogLevel": {
      "AssetManagement.Infrastructure.Services.AssetSearchService": "Information"
    }
  }
}
```

## Usage Examples

### Basic Search
```
Search: "4ZVV1V3"
Result: Finds assets with service tag 4ZVV1V3
```

### Exact Phrase
```
Search: "Courtroom 04 - A"
Result: Finds assets with exact desk location
```

### Prefix Search
```
Search: "OATHLIHRO"
Result: Finds assets with service tags starting with OATHLIHRO
```

### Combined Filters
```
Search: "computer"
Category: "Computer"
Location: "Main Office"
Status: "Active"
Result: Active computers in Main Office containing "computer"
```

### Advanced Filters
```
Search: "Dell"
Vendor: "Dell"
Unassigned Only: true
Created From: 2024-01-01
Result: Unassigned Dell equipment created after Jan 1, 2024
```

## Performance Considerations

### Indexing Strategy
- **Full-Text Index**: Primary search performance
- **Nonclustered Indexes**: Filter performance
- **Computed Column**: Fallback search performance

### Query Optimization
- **Server-side Pagination**: Limits result set size
- **Efficient Filtering**: Uses indexed columns
- **Lazy Loading**: Loads only required data

### Caching
- **FTS Availability**: Cached per application instance
- **Filter Options**: Loaded once per session
- **Search Results**: Not cached (real-time data)

## Troubleshooting

### Common Issues

#### FTS Not Available
**Symptoms**: Search falls back to LIKE queries
**Solution**: Install SQL Server with Advanced Services or use fallback search

#### Slow Search Performance
**Symptoms**: Search takes > 500ms
**Solutions**:
- Verify indexes are created
- Check FTS catalog status
- Monitor query execution plans

#### Missing Results
**Symptoms**: Expected assets not found
**Solutions**:
- Check field values for null/empty
- Verify search term spelling
- Test with simpler queries

### Debug Information
Search operations log detailed information:
- Query terms and filters
- Result counts and timing
- FTS vs fallback usage
- Performance metrics

## Future Enhancements

### Planned Features
- **Fuzzy Matching**: Levenshtein distance for typos
- **Search Suggestions**: Auto-complete functionality
- **Saved Searches**: User-specific search templates
- **Search Analytics**: Usage patterns and optimization

### Performance Improvements
- **Elasticsearch Integration**: For very large datasets
- **Redis Caching**: For frequently searched terms
- **Query Optimization**: Advanced indexing strategies

## Security Considerations

### Input Validation
- **Parameterized Queries**: Prevents SQL injection
- **Input Sanitization**: Handles special characters
- **Access Control**: Role-based search permissions

### Data Protection
- **Field-level Security**: Sensitive data filtering
- **Audit Logging**: Search activity tracking
- **Rate Limiting**: Prevents search abuse

---

For technical support or feature requests, please refer to the development team or create an issue in the project repository.

