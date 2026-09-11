export interface Post {
  id: number;
  createdAt: string;
  updatedAt: string;
  type: string;
  title: string | null;
  body: string | null;
}
