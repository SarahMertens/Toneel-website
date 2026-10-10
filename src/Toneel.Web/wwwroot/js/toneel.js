// The only JavaScript in the app: the browser APIs that C# cannot call
// directly. Loaded on demand by Services/BrowserFiles.cs.

/** Offers bytes as a file download. */
export function downloadFile(fileName, contentType, bytes) {
  const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
  save(url, fileName);
  URL.revokeObjectURL(url);
}

// Where each line goes on the 1240 × 584 ticket image, in pixels.
const TICKET_TEXT = {
  date: { x: 695, y: 311 },
  location: { x: 690, y: 352 },
  name: { x: 860, y: 392 },
};

/** Draws date, location and name on the ticket image and downloads the result as PNG. */
export async function downloadTicket(imageUrl, fileName, date, location, name) {
  const image = new Image();
  image.src = imageUrl;
  await image.decode(); // rejects when the image cannot be loaded

  const canvas = document.createElement('canvas');
  canvas.width = image.naturalWidth;
  canvas.height = image.naturalHeight;

  const context = canvas.getContext('2d');
  context.drawImage(image, 0, 0);
  context.font = '28px Arial, sans-serif';
  context.fillStyle = 'black';
  context.textAlign = 'center';

  // The fourth argument is a maximum width: a long name is squeezed
  // instead of running off the dotted line.
  context.fillText(date, TICKET_TEXT.date.x, TICKET_TEXT.date.y, 500);
  context.fillText(location, TICKET_TEXT.location.x, TICKET_TEXT.location.y, 500);
  context.fillText(name, TICKET_TEXT.name.x, TICKET_TEXT.name.y, 420);

  save(canvas.toDataURL('image/png'), fileName);
}

function save(url, fileName) {
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.append(link);
  link.click();
  link.remove();
}
