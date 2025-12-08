const express = require('express')
const { getAllPosts } = require('./db')

const app = express()
const port = 3000

app.get('/', (req, res) => {
    res.send('Hello World!')
})

app.get('/posts', (req, res) => {
    const posts = getAllPosts()
    res.json(posts)
})

app.listen(port, () => {
    console.log(`Example app listening on port ${port}`)
})
