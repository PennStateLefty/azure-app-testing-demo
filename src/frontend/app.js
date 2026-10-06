const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
let count = 0;
let total = 0;

function updateCart() {
  document.querySelector('#cart').textContent = `${count} items — ${currency.format(total)}`;
}

document.querySelector('#clear').addEventListener('click', () => {
  count = 0;
  total = 0;
  updateCart();
});

async function loadProducts() {
  const status = document.querySelector('#status');
  try {
    const response = await fetch('/api/products');
    if (!response.ok) throw new Error('Products request failed');
    const products = await response.json();
    for (const product of products) {
      const card = document.createElement('article');
      const title = document.createElement('h3');
      title.textContent = product.name;
      const price = document.createElement('p');
      price.textContent = currency.format(product.price);
      const button = document.createElement('button');
      button.type = 'button';
      button.textContent = `Add ${product.name}`;
      button.addEventListener('click', () => {
        count += 1;
        total += product.price;
        updateCart();
      });
      card.append(title, price, button);
      document.querySelector('#products').append(card);
    }
    status.textContent = 'Products ready';
  } catch {
    status.textContent = 'Unable to load products. Please reload to try again.';
  }
}

loadProducts();
