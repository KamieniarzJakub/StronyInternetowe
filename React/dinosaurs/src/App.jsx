import { useState } from "react";
import dinoData from "./dinosaurs.json";
import "./App.css";

function DinoCard({ item, onImageClick, onRatingChange, onImageUpload, onDelete }) {
  const [hoverRating, setHoverRating] = useState(0);

  return (
    <div className="dino-card">
      <img
        src={item.image}
        alt={item.name}
        onClick={() => onImageClick(item.image)}
        style={{ cursor: "pointer" }}
      />
      <div className="card-content">
        <h2>{item.name}</h2>
        <p>{item.description}</p>
        
        <div className="card-buttons">
        <div
          className="stars"
          onMouseLeave={() => setHoverRating(0)}
        >
          {Array.from({ length: 5 }).map((_, i) => {
            const index = i + 1;
            return (
              <span
                key={index}
                className={`star ${index <= (hoverRating || item.rating) ? "filled" : ""}`}
                onClick={() => onRatingChange(item.name, index - item.rating)}
                onMouseEnter={() => setHoverRating(index)}
              >
                ★
              </span>
            );
          })}
        </div>
        <label className="change-image">
          Change image
          <input
            type="file"
            accept="image/*"
            onChange={(e) => onImageUpload(e, true, item.name)}
            style={{ display: "none" }}
          />
        </label>
          <button
            onClick={() => onDelete(item.name)}
            className="delete-button"
          >
            Delete
          </button>
        </div>
      </div>
    </div>
  );
}

export default function DinoApp() {
  const [items, setItems] = useState(dinoData);
  const [search, setSearch] = useState("");
  const [sortKey, setSortKey] = useState("name");
  const [newItem, setNewItem] = useState({
    name: "",
    description: "",
    image: "",
    rating: 1,
  });
  const [fullscreenImage, setFullscreenImage] = useState(null);

  const handleImageClick = (src) => {
    setFullscreenImage(src);
  };

  const closeFullscreen = () => {
    setFullscreenImage(null);
  };

  const handleAdd = () => {
    if (!newItem.name || !newItem.description || !newItem.image) {
      alert("Please complete all fields before adding a dino.");
      return;
    }
    setItems([...items, { ...newItem, rating: parseInt(newItem.rating) }]);
    setNewItem({ name: "", description: "", image: "", rating: 1 });
  };

  const handleDelete = (name) => {
    setItems(items.filter((item) => item.name !== name));
  };

  const handleRatingChange = (name, delta) => {
    setItems(
      items.map((item) =>
        item.name === name
          ? { ...item, rating: Math.max(1, Math.min(5, item.rating + delta)) }
          : item
      )
    );
  };

  const handleImageUpload = (e, isEdit = false, name = "") => {
    const file = e.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onloadend = () => {
        if (isEdit) {
          setItems(
            items.map((item) =>
              item.name === name ? { ...item, image: reader.result } : item
            )
          );
        } else {
          setNewItem({ ...newItem, image: reader.result });
        }
      };
      reader.readAsDataURL(file);
    }
  };

  const handleDownload = () => {
    const json = JSON.stringify(items, null, 2);
    const blob = new Blob([json], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "dinosaurs.json";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const handleJsonUpload = (e) => {
    const file = e.target.files[0];
    if (!file) return;
  
    const reader = new FileReader();
    reader.onload = (event) => {
      try {
        const json = JSON.parse(event.target.result);
        if (Array.isArray(json)) {
          setItems(json);
        } else {
          alert("Invalid JSON format. Expected an array.");
        }
      } catch (err) {
        alert("Error parsing JSON file.");
        console.error(err);
      }
    };
    reader.readAsText(file);
  };
  

  const filtered = items
    .filter((item) => item.name.toLowerCase().includes(search.toLowerCase()))
    .sort((a, b) => {
      if (sortKey === "rating") return b.rating - a.rating;
      return a.name.localeCompare(b.name);
    });

  return (
    <div className="container">
      <h1>🦕 Ark Dinosaurs</h1>

      <div className="add-section">
        <h2>Add New Dino</h2>
        <div className="add-form">
          <input
            type="text"
            placeholder="Name"
            value={newItem.name}
            onChange={(e) => setNewItem({ ...newItem, name: e.target.value })}
          />
          <label className="upload-image">
            Image:
            <input
              type="file"
              accept="image/*"
              onChange={handleImageUpload}
            />
          </label>
          <input
            type="text"
            placeholder="Description"
            value={newItem.description}
            onChange={(e) => setNewItem({ ...newItem, description: e.target.value })}
          />
          <label className="star-rating">
            Rating: 
            <input
              type="number"
              min={1}
              max={5}
              value={newItem.rating}
              onChange={(e) => setNewItem({ ...newItem, rating: e.target.value })}
            />
          </label>
          <button onClick={handleAdd} className="add-button">
            Add Dino
          </button>
        </div>
      </div>
      
      <h2>Search for dino</h2>
      <div className="search-sort">
        <input
          type="text"
          placeholder="Search dinos..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select value={sortKey} onChange={(e) => setSortKey(e.target.value)}>
          <option value="name">Sort by Name</option>
          <option value="rating">Sort by Rating</option>
        </select>
        <button onClick={handleDownload} className="download-button">
          📥 Download JSON
        </button>
        <label className="upload-button">
          📤 Load JSON
          <input
            type="file"
            accept="application/json"
            onChange={handleJsonUpload}
            style={{ display: "none" }}
          />
        </label>
      </div>

      <h2>My collection</h2>
      <div className="dino-grid">
        {filtered.map((item) => (
          <DinoCard
            key={item.name}
            item={item}
            onImageClick={handleImageClick}
            onRatingChange={handleRatingChange}
            onImageUpload={handleImageUpload}
            onDelete={handleDelete}
          />
        ))}
      </div>

      {fullscreenImage && (
        <div className="fullscreen-overlay" onClick={closeFullscreen}>
          <img src={fullscreenImage} alt="Fullscreen Dino" />
        </div>
      )}
    </div>
  );
}
