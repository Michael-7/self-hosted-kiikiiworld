import { useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { Link, Navigate } from 'react-router-dom';
import styles from './NewPost.module.css';
import { useAuth } from '../../hooks/useAuth';
import { API_URL } from '../../lib/api';

type NewPostType = 'Quote' | 'Story' | 'Photo';

const MAX_IMAGES = 10;

export function NewPost() {
  const { status } = useAuth();
  const [type, setType] = useState<NewPostType>('Quote');
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [images, setImages] = useState<File[]>([]);
  const [imageInputKey, setImageInputKey] = useState(0);
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

    try {
      const res = await fetch(`${API_URL}/posts/`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ type, title, body }),
      });

      if (!res.ok) {
        setError(res.status === 401 ? 'Session expired. Please log in again.' : 'Failed to create post.');
        return;
      }

      const post = await res.json();

      for (const file of images) {
        const formData = new FormData();
        formData.append('file', file);

        const mediaRes = await fetch(`${API_URL}/posts/${post.id}/media`, {
          method: 'POST',
          credentials: 'include',
          body: formData,
        });

        if (!mediaRes.ok) {
          await fetch(`${API_URL}/posts/${post.id}`, { method: 'DELETE', credentials: 'include' });
          setError('Image upload failed, so the post was not created. Please try again.');
          return;
        }
      }

      setTitle('');
      setBody('');
      setImages([]);
      setImageInputKey((key) => key + 1);
      setSuccess(true);
    } catch {
      setError('Network error. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleImagesChange = (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? []);
    setError(files.length > MAX_IMAGES ? `You can attach up to ${MAX_IMAGES} images.` : null);
    setImages(files.slice(0, MAX_IMAGES));
  };

  const removeImage = (index: number) => {
    setImages((current) => current.filter((_, i) => i !== index));
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
          <select
            value={type}
            onChange={(event) => {
              const newType = event.target.value as NewPostType;
              setType(newType);
              if (newType === 'Photo') {
                setBody('');
              } else {
                setImages([]);
                setImageInputKey((key) => key + 1);
              }
            }}
          >
            <option value="Photo">Photo</option>
            <option value="Quote">Quote</option>
            <option value="Story">Story</option>
          </select>
        </label>

        <label className={styles.field}>
          <span>Title</span>
          <input type="text" value={title} onChange={(event) => setTitle(event.target.value)} maxLength={200} required />
        </label>

        {(type === 'Quote' || type === 'Story') && (
          <label className={styles.field}>
            <span>Body</span>
            <textarea value={body} onChange={(event) => setBody(event.target.value)} rows={10} required />
          </label>
        )}

        {type === 'Photo' && (
          <label className={styles.field}>
            <span>Images (up to {MAX_IMAGES})</span>
            <input key={imageInputKey} type="file" accept="image/*" multiple onChange={handleImagesChange} />
          </label>
        )}

        {type === 'Photo' && images.length > 0 && (
          <ul className={styles.imageList}>
            {images.map((file, index) => (
              <li key={`${file.name}-${index}`} className={styles.imageItem}>
                <span className={styles.imageName}>{file.name}</span>
                <button type="button" className={styles.imageRemove} onClick={() => removeImage(index)}>
                  Remove
                </button>
              </li>
            ))}
          </ul>
        )}

        {error && <p className={styles.error}>{error}</p>}
        {success && <p className={styles.success}>Post created.</p>}

        <button type="submit" className={styles.submit} disabled={submitting}>
          {submitting ? 'Posting…' : 'Create post'}
        </button>
      </form>
    </div>
  );
}
