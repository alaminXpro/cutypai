# SSO Setup Guide

## Google OAuth Configuration

### 1. Google Cloud Console Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google+ API (or Google Identity API)
4. Go to "Credentials" → "Create Credentials" → "OAuth 2.0 Client IDs"
5. Set Application type to "Web application"
6. Add authorized JavaScript origins:
   - `http://localhost:3000` (development)
   - `https://yourdomain.com` (production)
7. Copy the Client ID

### 2. Environment Variables

#### Frontend (.env.local)

```bash
NEXT_PUBLIC_GOOGLE_CLIENT_ID=479425469861-ctkj7fd1pqogeaq079dlr4lmclisa37o.apps.googleusercontent.com
NEXT_PUBLIC_API_BASE=http://localhost:5000
```

#### Backend (Environment Variable)

```bash
# Set environment variable
export Google_CLIENT_ID=479425469861-ctkj7fd1pqogeaq079dlr4lmclisa37o.apps.googleusercontent.com

# Or in .env file (if using DotNetEnv)
Google_CLIENT_ID=479425469861-ctkj7fd1pqogeaq079dlr4lmclisa37o.apps.googleusercontent.com
```

### 3. Testing

1. Start the backend: `dotnet run`
2. Start the frontend: `cd clientApp && npm run dev`
3. Navigate to your login page
4. Click "Continue with Google"
5. Complete Google OAuth flow

### 4. Production Deployment

1. Update Google OAuth settings with production domain
2. Set environment variables in production
3. Ensure HTTPS is enabled
4. Test the complete flow

## Troubleshooting

### Common Issues:

1. **"NEXT_PUBLIC_GOOGLE_CLIENT_ID is not set"**

   - Check your .env.local file
   - Restart your development server

2. **"Invalid Google token"**

   - Verify Google Client ID matches in both frontend and backend
   - Check Google Cloud Console settings

3. **CORS Issues**

   - Ensure backend CORS is configured for your frontend domain
   - Check API_BASE environment variable

4. **User not created**
   - Check MongoDB connection
   - Verify UserRepository.CreateFromSsoAsync is working
   - Check backend logs for errors
