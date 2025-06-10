const express = require('express');
const session = require('express-session');
const bodyParser = require('body-parser');
const mysql = require('mysql2/promise');

const app = express();
const port = 3000;

const pool = mysql.createPool({
    host: 'localhost',
    user: 'website',
    password: '',
    database: 'gamestore',
    waitForConnections: true,
    connectionLimit: 10,
    queueLimit: 0
});

app.use(bodyParser.urlencoded({ extended: false }));
app.use(express.static('public'));
app.set('view engine', 'ejs');

app.use(session({
    secret: 'storesecret',
    resave: false,
    saveUninitialized: true
}));

app.use((req, res, next) => {
    if (!req.session.cart) req.session.cart = [];
    next();
});

app.get('/', async (req, res) => {
    const [results] = await pool.query('SELECT * FROM products');
    const cartCount = req.session.cart.length;
    const cartIds = req.session.cart;
    const message = req.session.message;
    delete req.session.message;
    res.render('index', { products: results, message, cartCount, cartIds });
});

app.post('/add-to-cart', (req, res) => {
    const productId = parseInt(req.body.productId);
    if (!req.session.cart.includes(productId)) {
        req.session.cart.push(productId);
    }
    res.redirect('/');
});

app.get('/cart', async (req, res) => {
    const cart = req.session.cart;
    const message = req.session.message;
    delete req.session.message;

    if (cart.length === 0) {
        return res.render('cart', { items: [], message, cartCount: 0 });
    }

    const [results] = await pool.query('SELECT * FROM products WHERE id IN (?)', [cart]);

    const availableIds = results.map(p => p.id);
    const unavailableIds = cart.filter(id => !availableIds.includes(id));

    if (unavailableIds.length > 0) {
        req.session.cart = cart.filter(id => availableIds.includes(id));
        req.session.message = 'Some items were removed from your cart as they are no longer available.';
        return res.redirect('/cart');
    }

    res.render('cart', {
        items: results,
        message,
        cartCount: req.session.cart.length
    });
});

app.post('/remove-from-cart', (req, res) => {
    const productId = parseInt(req.body.productId);
    req.session.cart = req.session.cart.filter(id => id !== productId);
    res.redirect('/cart');
});

app.post('/cancel', (req, res) => {
    req.session.cart = [];
    req.session.message = 'Purchase cancelled.';
    res.redirect('/');
});

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

app.post('/checkout', async (req, res) => {
    const cart = [...req.session.cart];
    const connection = await pool.getConnection();

    try {
        await connection.beginTransaction();

        const [rows] = await connection.query(
            'SELECT id FROM products WHERE id IN (?) FOR UPDATE',
            [cart]
        );

        const availableIds = rows.map(p => p.id);
        const unavailable = cart.filter(id => !availableIds.includes(id));

        //await sleep(10000);

        if (unavailable.length > 0) {
            req.session.cart = cart.filter(id => !unavailable.includes(id));
            req.session.message = 'Purchase CANCELED - Some items were already sold.';
            await connection.rollback();
            return res.redirect('/cart');
        }

        await connection.query('DELETE FROM products WHERE id IN (?)', [cart]);
        await connection.commit();

        req.session.cart = [];
        req.session.message = 'Purchase successful!';
        res.redirect('/');
    } catch (err) {
        await connection.rollback();
        console.error(err);
        res.status(500).send('Internal Server Error');
    } finally {
        connection.release();
    }
});

app.listen(port, () => {
    console.log(`Game Store running at http://localhost:${port}`);
});
