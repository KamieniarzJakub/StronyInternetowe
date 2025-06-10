const request = require('request-promise');
const { JSDOM } = require('jsdom');
const fs = require('fs'); // Dodajemy moduł do zapisu do pliku

const baseURL = 'https://www.lego.com/pl-pl/categories/';
const filter = '&filters.i0.key=variants.attributes.availabilityStatus.zxx-PL&filters.i0.values.i0="E_AVAILABLE"'

const categories = [
    'price-under-50-pln',
    'price-50-100-pln',
    'price-100-200-pln',
    'price-200-500-pln',
    'price-over-500-pln'
];

async function fetchLegoData() {
    try {
        let products = [];

        for (let category of categories) {
            console.log(`Scraping category: ${category}...`);

            let page = 1;
            let categoryProducts = [];

            while (true) {
                console.log(`Scraping page ${page} of category ${category}...`);

                let pageData = await request(`${baseURL}${category}?page=${page}${filter}`);
                
                // Parse the HTML data
                const pageDom = new JSDOM(pageData);
                const pageDocument = pageDom.window.document;

                const productElements = pageDocument.querySelectorAll('.Grid_grid-item__FLJlN');

                if (productElements.length === 0) {
                    console.log(`No products found on page ${page} of category ${category}. Stopping.`);
                    break;
                }

                productElements.forEach(element => {
                    let name = element.querySelector('.ProductLeaf_title__1UhfJ .markup')?.textContent.trim();
                    let priceText = element.querySelector('.ProductLeaf_priceRow__RUx3P .ds-label-md-bold')?.textContent.trim();
                    let price = priceText ? parseFloat(priceText.replace(/\D/g, '')) / 100 : 0;

                    let piecesText = element.querySelectorAll('.ds-label-sm-medium.ProductLeaf_attributeLabel__2VyjW')[1]?.textContent.trim();
                    let pieces = piecesText ? parseInt(piecesText, 10) : 0;

                    if (!isNaN(price) && !isNaN(pieces) && pieces > 50) {
                        let unitPrice = price / pieces;
                        categoryProducts.push({ name, price, pieces, unitPrice, category });
                    }
                });
                page++;
                await new Promise(resolve => setTimeout(resolve, 50));
            }
            products = [...products, ...categoryProducts];
        }

        if (products.length > 0) {
            products.sort((a, b) => a.unitPrice - b.unitPrice);

            console.log("Ranked LEGO Products by Unit Price:");
            products.forEach(product => {
                console.log(`${product.name} - Price: ${product.price} PLN, Pieces: ${product.pieces}, Unit Price: ${product.unitPrice.toFixed(2)} PLN/piece, Category: ${product.category}`);
            });

            const averageUnitPrice = products.reduce((sum, product) => sum + product.unitPrice, 0) / products.length;
            console.log(`\nAverage unit price: ${averageUnitPrice.toFixed(2)} PLN/piece`);

            fs.writeFileSync('lego.json', JSON.stringify(products, null, 2), 'utf-8');
            console.log("\nResults saved into: lego.json");
        } else {
            console.log("No products found.");
        }

    } catch (error) {
        console.error("Error fetching LEGO data:", error);
    }
}

fetchLegoData();
