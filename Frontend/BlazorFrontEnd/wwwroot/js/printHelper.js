window.imprimirHistoriaClinica = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element) {
        window.print();
        return;
    }

    // Clonar elemento para normalizar URLs absolutas de imágenes
    const clone = element.cloneNode(true);
    const originalImages = element.querySelectorAll('img');
    const clonedImages = clone.querySelectorAll('img');

    clonedImages.forEach((img, idx) => {
        const orig = originalImages[idx];
        if (orig && orig.src) {
            img.src = orig.src;
        } else {
            const rawSrc = img.getAttribute('src');
            if (rawSrc && !rawSrc.startsWith('http') && !rawSrc.startsWith('data:')) {
                img.src = window.location.origin + (rawSrc.startsWith('/') ? '' : '/') + rawSrc;
            }
        }
    });

    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = '0';
    iframe.id = 'print-iframe-' + Date.now();
    document.body.appendChild(iframe);

    const doc = iframe.contentWindow.document;
    doc.open();
    doc.write(`
        <!DOCTYPE html>
        <html lang="es">
        <head>
            <meta charset="utf-8" />
            <base href="${window.location.origin}/" />
            <title>Historia Clínica - Veterinaria Ñandubay</title>
            <style>
                @page {
                    size: A4 portrait;
                    margin: 12mm 15mm 15mm 15mm;
                }
                * {
                    box-sizing: border-box;
                    margin: 0;
                    padding: 0;
                }
                body {
                    font-family: 'Segoe UI', Arial, Helvetica, sans-serif;
                    font-size: 10pt;
                    color: #1e293b;
                    background: #ffffff;
                    line-height: 1.4;
                    padding: 8px;
                    -webkit-print-color-adjust: exact !important;
                    print-color-adjust: exact !important;
                }
                .historia-print-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    border-bottom: 2.5px solid #00A36C;
                    padding-bottom: 12px;
                    margin-bottom: 16px;
                }
                .historia-print-logo-box {
                    display: flex;
                    align-items: center;
                    gap: 14px;
                }
                .historia-print-logo {
                    width: 65px;
                    height: 65px;
                    object-fit: cover;
                    border-radius: 6px;
                    border: 1px solid #cbd5e1;
                    display: block;
                }
                .historia-print-title {
                    font-size: 17pt;
                    font-weight: 700;
                    color: #00A36C;
                    margin: 0;
                    line-height: 1.1;
                }
                .historia-print-subtitle {
                    font-size: 9pt;
                    color: #64748b;
                    margin-top: 3px;
                }
                .historia-print-meta {
                    text-align: right;
                    font-size: 8.5pt;
                    color: #475569;
                }
                .historia-print-badge {
                    font-size: 10.5pt;
                    font-weight: 700;
                    color: #00A36C;
                    margin-bottom: 4px;
                    letter-spacing: 0.5px;
                }
                .historia-print-section {
                    margin-bottom: 16px;
                    page-break-inside: auto;
                    break-inside: auto;
                }
                .historia-print-section-header {
                    font-size: 11pt;
                    font-weight: 700;
                    color: #00A36C;
                    border-bottom: 1.5px solid #00A36C;
                    padding-bottom: 3px;
                    margin-bottom: 8px;
                    text-transform: uppercase;
                    letter-spacing: 0.5px;
                }
                .historia-print-info-table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-bottom: 8px;
                    font-size: 9pt;
                }
                .historia-print-info-table td {
                    padding: 5px 8px;
                    border: 1px solid #cbd5e1;
                    vertical-align: top;
                }
                .historia-print-info-table td strong {
                    color: #0f172a;
                }
                .historia-print-empty {
                    font-size: 9pt;
                    color: #64748b;
                    font-style: italic;
                    margin: 4px 0 8px 0;
                    padding: 6px;
                    background: #f8fafc;
                    border: 1px dashed #cbd5e1;
                    border-radius: 4px;
                }
                .historia-print-consult-card {
                    page-break-inside: avoid;
                    break-inside: avoid;
                    border: 1px solid #cbd5e1;
                    border-radius: 4px;
                    margin-bottom: 8px;
                    padding: 8px 10px;
                    background: #f8fafc;
                    font-size: 9pt;
                }
                .historia-print-consult-header {
                    display: flex;
                    justify-content: space-between;
                    border-bottom: 1px solid #e2e8f0;
                    padding-bottom: 4px;
                    margin-bottom: 6px;
                    font-size: 9.5pt;
                    color: #0f172a;
                }
                .historia-print-row {
                    display: flex;
                    gap: 20px;
                    margin-bottom: 4px;
                }
                .historia-print-col {
                    font-size: 9pt;
                }
                .historia-print-field {
                    margin-top: 4px;
                    font-size: 9pt;
                    line-height: 1.35;
                }
                .historia-print-table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-bottom: 8px;
                    font-size: 8.5pt;
                    page-break-inside: auto;
                    break-inside: auto;
                }
                .historia-print-table th {
                    background-color: #f1f5f9;
                    color: #0f172a;
                    font-weight: 700;
                    border: 1px solid #cbd5e1;
                    padding: 5px 7px;
                    text-align: left;
                }
                .historia-print-table td {
                    border: 1px solid #cbd5e1;
                    padding: 5px 7px;
                    vertical-align: top;
                    color: #1e293b;
                }
                .historia-print-table tr {
                    page-break-inside: avoid;
                    break-inside: avoid;
                }
                .historia-print-footer {
                    margin-top: 25px;
                    page-break-inside: avoid;
                    break-inside: avoid;
                }
                .historia-print-signatures {
                    display: flex;
                    justify-content: space-between;
                    margin-top: 35px;
                    margin-bottom: 20px;
                    padding: 0 40px;
                }
                .historia-print-sig-box {
                    text-align: center;
                    width: 200px;
                }
                .historia-print-sig-line {
                    border-top: 1px solid #475569;
                    margin-bottom: 4px;
                }
                .historia-print-sig-box p {
                    font-size: 8.5pt;
                    color: #475569;
                    margin: 0;
                }
                .historia-print-legal {
                    text-align: center;
                    font-size: 7.5pt;
                    color: #94a3b8;
                    border-top: 1px solid #e2e8f0;
                    padding-top: 8px;
                }
            </style>
        </head>
        <body>
            ${clone.innerHTML}
        </body>
        </html>
    `);
    doc.close();

    let printTriggered = false;
    const triggerPrint = () => {
        if (printTriggered) return;
        printTriggered = true;
        iframe.contentWindow.focus();
        iframe.contentWindow.print();
        setTimeout(() => {
            if (document.body.contains(iframe)) {
                document.body.removeChild(iframe);
            }
        }, 2000);
    };

    const imgs = Array.from(iframe.contentWindow.document.images);
    if (imgs.length === 0) {
        setTimeout(triggerPrint, 250);
    } else {
        let loaded = 0;
        const checkDone = () => {
            loaded++;
            if (loaded >= imgs.length) {
                setTimeout(triggerPrint, 250);
            }
        };

        imgs.forEach(img => {
            if (img.complete && img.naturalWidth > 0) {
                checkDone();
            } else {
                img.addEventListener('load', checkDone);
                img.addEventListener('error', checkDone);
            }
        });

        // Safety fallback timeout in case of network stall
        setTimeout(triggerPrint, 1500);
    }
};
