export interface PostMedia {
  id: number;
  type: string;
  url: string;
  originalUrl: string | null;
}

export interface Post {
  id: number;
  createdAt: string;
  updatedAt: string;
  type: string;
  title: string | null;
  body: string | null;
  media: PostMedia[];
}
