import { useEffect, useState } from 'react';
import styles from './Posts.module.css';

interface Post {
  id: number;
  createdAt: string;
  updatedAt: string;
  type: string;
  title: string | null;
  body: string | null;
}

const API_URL = 'http://localhost:5199';

export function Posts() {
  const [posts, setPosts] = useState<Post[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch(`${API_URL}/posts`)
      .then((res) => {
        if (!res.ok) throw new Error(`Request failed: ${res.status}`);
        return res.json();
      })
      .then((data) => setPosts(data))
      .catch((err) => setError(err.message));
  }, []);

  if (error) {
    return <div className={styles.postsWrapper}>Failed to load posts: {error}</div>;
  }

  return (
    <div className={styles.postsWrapper}>
      {posts.map((post) => (
        <div key={post.id} className={styles.post}>
          <p>{post.type}</p>
          {post.title && <p>{post.title}</p>}
          {post.body && <p>{post.body}</p>}
        </div>
      ))}
    </div>
  );
}
