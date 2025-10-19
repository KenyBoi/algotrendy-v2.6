// COMPLETED: Added navigation links to test page
// COMPLETED: Added back to home link and quick navigation buttons
// TODO: Add more comprehensive debugging information
// TODO: Add API connection test functionality
// TODO: Add database connection status check
import Link from 'next/link';

export default function TestPage() {
    return (
        <div style={{ padding: '20px', backgroundColor: '#7997a1', color: 'black' }}>
            <div style={{ marginBottom: '20px' }}>
                <Link href="/" style={{ color: 'blue', textDecoration: 'underline', fontSize: '16px' }}>
                    ← Back to Home
                </Link>
            </div>
            <h1>🎯 white PAGE - AlgoTrendy Frontend</h1>
            <p>If you see this, Next.js is working!</p>
            <p>Date: {new Date().toLocaleString()}</p>
            <div style={{ border: '2px solid red', padding: '10px', margin: '10px 0' }}>
                <strong>DEBUGGING INFO:</strong>
                <br />• Next.js: WORKING
                <br />• React: WORKING
                <br />• Server: RUNNING
            </div>
            <div style={{ marginTop: '20px', display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                <Link href="/dashboard" style={{ padding: '10px 20px', backgroundColor: '#4CAF50', color: 'white', textDecoration: 'none', borderRadius: '5px' }}>
                    Go to Dashboard
                </Link>
                <Link href="/search" style={{ padding: '10px 20px', backgroundColor: '#2196F3', color: 'white', textDecoration: 'none', borderRadius: '5px' }}>
                    Go to Search
                </Link>
                <Link href="/dev-systems" style={{ padding: '10px 20px', backgroundColor: '#FF9800', color: 'white', textDecoration: 'none', borderRadius: '5px' }}>
                    Go to Dev Systems
                </Link>
                <Link href="/login" style={{ padding: '10px 20px', backgroundColor: '#9C27B0', color: 'white', textDecoration: 'none', borderRadius: '5px' }}>
                    Go to Login
                </Link>
            </div>
        </div>
    );
}