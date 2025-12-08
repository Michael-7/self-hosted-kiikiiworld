const fs = require('fs')
const path = require('path')
const Database = require('better-sqlite3')

// Prepare database location and make sure the directory exists
const dataDir = path.join(__dirname, '..', 'data')
fs.mkdirSync(dataDir, { recursive: true })
const dbPath = path.join(dataDir, 'kiikii.db')

const db = new Database(dbPath)
db.pragma('foreign_keys = ON')

// Ensure schema is present
db.exec(`
CREATE TABLE IF NOT EXISTS Type (
    value TEXT PRIMARY KEY,
    label TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Post (
    id INTEGER PRIMARY KEY,
    type TEXT NOT NULL,
    date TEXT NOT NULL,
    title TEXT NOT NULL,
    hidden INTEGER NOT NULL CHECK (hidden IN (0, 1)),
    markdown_body TEXT,
    url TEXT,
    FOREIGN KEY (type) REFERENCES Type(value) ON UPDATE CASCADE ON DELETE RESTRICT
);
`)

// Seed the database with example data if it is empty
const seedIfEmpty = () => {
    const typeCount = db.prepare('SELECT COUNT(*) AS count FROM Type').get().count
    const postCount = db.prepare('SELECT COUNT(*) AS count FROM Post').get().count

    if (typeCount > 0 || postCount > 0) return

    const seedTypes = [
        { value: 'article', label: 'Article' },
        { value: 'update', label: 'Update' },
        { value: 'announcement', label: 'Announcement' }
    ]

    const seedPosts = [
        {
            id: 1,
            type: 'article',
            date: '2024-01-15T10:00:00Z',
            title: 'Welcome to Kiikiiworld',
            hidden: 0,
            markdown_body: '# Hello World\nThis is our first post in **Kiikiiworld**.',
            url: 'https://example.com/welcome'
        },
        {
            id: 2,
            type: 'update',
            date: '2024-03-05T15:30:00Z',
            title: 'Platform Update',
            hidden: 0,
            markdown_body: 'We shipped a few improvements and bug fixes.',
            url: 'https://example.com/platform-update'
        },
        {
            id: 3,
            type: 'announcement',
            date: '2024-05-20T08:00:00Z',
            title: 'New Features Coming Soon',
            hidden: 1,
            markdown_body: 'Sneak peek of upcoming features.',
            url: null
        }
    ]

    const insertType = db.prepare('INSERT INTO Type (value, label) VALUES (@value, @label)')
    const insertPost = db.prepare(`
        INSERT INTO Post (id, type, date, title, hidden, markdown_body, url)
        VALUES (@id, @type, @date, @title, @hidden, @markdown_body, @url)
    `)

    const seedTransaction = db.transaction(() => {
        seedTypes.forEach(type => insertType.run(type))
        seedPosts.forEach(post => insertPost.run(post))
    })

    seedTransaction()
}

seedIfEmpty()

function getAllPosts() {
    const stmt = db.prepare(`
        SELECT 
            Post.id,
            Post.type,
            Type.label AS type_label,
            Post.date,
            Post.title,
            Post.hidden,
            Post.markdown_body,
            Post.url
        FROM Post
        LEFT JOIN Type ON Type.value = Post.type
        ORDER BY datetime(Post.date) DESC
    `)

    return stmt.all().map(row => ({
        id: row.id,
        type: {
            value: row.type,
            label: row.type_label ?? row.type
        },
        date: row.date,
        title: row.title,
        hidden: Boolean(row.hidden),
        markdown_body: row.markdown_body,
        url: row.url
    }))
}

module.exports = { db, getAllPosts }
