import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate } from 'react-router-dom';
import styles from './NewPost.module.css';
import { useAuth } from '../../hooks/useAuth';
import { API_URL } from '../../lib/api';

type NewPostType = 'Quote' | 'Story';

export function NewPost() {
  const { status } = useAuth();
  const [type, setType] = useState<NewPostType>('Quote');
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  if (status === 'loading') {
    return <div className={styles.page} />;
  }

  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace />;
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    setSuccess(false);

    const res = await fetch(`${API_URL}/posts/`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ type, title, body }),
    });

    setSubmitting(false);

    if (!res.ok) {
      setError(res.status === 401 ? 'Session expired. Please log in again.' : 'Failed to create post.');
      return;
    }

    setTitle('');
    setBody('');
    setSuccess(true);
  };

  return (
    <div className={styles.page}>
      <form className={styles.form} onSubmit={handleSubmit}>
        <div className={styles.header}>
          <h1 className={styles.title}>New post</h1>
          <Link to="/login" className={styles.back}>
            Back
          </Link>
        </div>

        <label className={styles.field}>
          <span>Type</span>
          <select value={type} onChange={(event) => setType(event.target.value as NewPostType)}>
            <option value="Quote">Quote</option>
            <option value="Story">Story</option>
          </select>
        </label>

        <label className={styles.field}>
          <span>Title</span>
          <input type="text" value={title} onChange={(event) => setTitle(event.target.value)} maxLength={200} required />
        </label>

        <label className={styles.field}>
          <span>Body</span>
          <textarea value={body} onChange={(event) => setBody(event.target.value)} rows={10} required />
        </label>

        {error && <p className={styles.error}>{error}</p>}
        {success && <p className={styles.success}>Post created.</p>}

        <button type="submit" className={styles.submit} disabled={submitting}>
          {submitting ? 'Posting…' : 'Create post'}
        </button>
      </form>
    </div>
  );
}
