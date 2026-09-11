import { useEffect, useState } from 'react';
import styles from './Posts.module.css';
import type { Post as PostModel } from '../../types/post';
import { Post } from '../post/Post';
import { Menu } from '../menu/Menu';

const API_URL = 'http://localhost:5199';

export function Posts() {
  const [posts, setPosts] = useState<PostModel[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<string | null>(null);

  useEffect(() => {
    fetch(`${API_URL}/posts`)
      .then((res) => {
        if (!res.ok) throw new Error(`Request failed: ${res.status}`);
        return res.json();
      })
      .then((data) => setPosts(data))
      .catch((err) => setError(err.message));
  }, []);

  const types = [...new Set(posts.map((post) => post.type))];
  const shownPosts = filter ? posts.filter((post) => post.type === filter) : posts;

  return (
    <>
      <div className={styles.postContainer}>
        {error ? (
          <p>Failed to load posts: {error}</p>
        ) : (
          <div className={styles.postList}>
            {shownPosts.map((post) => (
              <Post key={post.id} post={post} />
            ))}
          </div>
        )}
      </div>
      <Menu types={types} selected={filter} onSelect={setFilter} />
    </>
  );
}
