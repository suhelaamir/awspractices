exports.handler = async (event, context) => {
    console.log("Smart logging");
    console.info("Info logging");
    console.warn("Warning logging");
    const body = `Function name: ${context.functionName}
    LogStream Name: ${context.logStreamName}
    `;
    return body;
}